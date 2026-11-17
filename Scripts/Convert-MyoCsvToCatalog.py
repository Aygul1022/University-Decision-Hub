"""Prepare a catalogue v2 candidate and an exhaustive audit; never connect to a DB."""
import argparse
import collections
import copy
import csv
import hashlib
import json
import re
import unicodedata
from decimal import Decimal, InvalidOperation
from pathlib import Path


def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def normalized(value):
    return " ".join(unicodedata.normalize("NFKC", value).casefold()
                    .replace("i\u0307", "i").split())


def number(value):
    if not value.strip():
        return None
    try:
        result = Decimal(value.strip().replace(",", "."))
    except InvalidOperation as error:
        raise ValueError("Invalid source number") from error
    if not result.is_finite() or result < 0:
        raise ValueError("Negative or nonfinite source number")
    return int(result) if result == result.to_integral_value() else float(result)


def stable_course(department, code):
    raw = json.dumps([department, code], ensure_ascii=False, separators=(",", ":"))
    return "CRS-MYO-" + hashlib.sha256(raw.encode()).hexdigest()[:32].upper()


def check_export(base, exported):
    """Reject a stale baseline rather than preparing against a different target export."""
    courses = {item["courseKey"]: item for item in base["courses"]}
    curricula = {item["curriculumKey"]: item for item in base["programmeCourses"]}
    if len(exported) != len(curricula):
        raise ValueError("Target curriculum export differs from baseline row count")
    seen = set()
    for item in exported:
        key = item["CurriculumKey"]
        if key in seen or key not in curricula:
            raise ValueError("Target export has duplicate or unknown curriculum key")
        seen.add(key)
        old = curricula[key]
        course = courses[old["courseKey"]]
        expected = {
            "ProgrammeKey": old["programmeKey"],
            "CourseDepartmentKey": course["departmentKey"],
            "CourseKey": course["courseKey"],
            "CourseCode": course["courseCode"],
            "CourseName": course["courseName"],
            **{field[0].upper() + field[1:]: old.get(field) for field in (
                "catalogueYear", "semester", "recommendedStudyYear", "isElective", "courseCredit", "ects")},
        }
        for field, value in expected.items():
            actual = item[field]
            if actual in ("", "NULL"):
                actual = None
            elif field == "IsElective":
                if actual.lower() not in ("true", "false"):
                    raise ValueError("Invalid boolean in target export")
                actual = actual.lower() == "true"
            elif field in ("Semester", "RecommendedStudyYear", "CourseCredit", "Ects"):
                actual = number(actual)
            if actual != value:
                raise ValueError(f"Target export differs from baseline: {key} / {field}")


def prepare(base, programmes_source, source):
    result = copy.deepcopy(base)
    programmes = {item["programmeKey"]: item for item in result["programmes"]}
    courses = {(item["departmentKey"], item["courseCode"].strip().upper()): item
               for item in result["courses"]}
    curricula = {item["curriculumKey"]: item for item in result["programmeCourses"]}
    programme_map = {}
    for item in programmes_source:
        source_id = item["bologna_program_id"]
        if source_id in programme_map:
            raise ValueError("Duplicate source Bologna programme ID")
        programme_map[source_id] = item
    keys = [item["v7_mufredat_anahtari"].strip() for item in source
            if item["v7_mufredat_anahtari"].strip()]
    if len(keys) != len(set(keys)):
        raise ValueError("Duplicate source curriculum key")

    # Assess all names first so conflicts do not depend on input ordering.
    names = collections.defaultdict(set)
    for item in source:
        mapping = programme_map.get(item["bologna_program_id"], {})
        programme = programmes.get(mapping.get("v7_program_anahtari_adayi"))
        if programme:
            identity = (programme["departmentKey"], item["ders_kodu"].strip().upper())
            names[identity].add(normalized(item["ders_adi"]))

    audit = []
    for index, item in enumerate(source, start=2):
        entry = {"csvRow": index, "programme": item["web_program"],
                 "courseCode": item["ders_kodu"], "sourceUrl": item["dogus_ders_karti"],
                 "curriculumKey": item["v7_mufredat_anahtari"], "status": "pending"}
        audit.append(entry)
        mapping = programme_map.get(item["bologna_program_id"], {})
        pk = mapping.get("v7_program_anahtari_adayi", "").strip()
        programme = programmes.get(pk)
        if not programme:
            entry["reason"] = "Programme source key missing or absent from baseline"
            continue
        if normalized(mapping["v7_program_adi_adayi"]) != normalized(programme["programmeName"]):
            entry["reason"] = "Programme candidate name differs from baseline"
            continue
        source_bologna = mapping["bologna_program_id"]
        if programme.get("bolognaProgrammeId") not in (None, "", source_bologna):
            entry["reason"] = "Conflicting programme Bologna identity"
            continue
        ck = item["v7_mufredat_anahtari"].strip()
        if not ck:
            entry["reason"] = "No stable curriculum source key"
            continue
        code, name = item["ders_kodu"].strip(), item["ders_adi"].strip()
        identity = (programme["departmentKey"], code.upper())
        if not code or len(code) > 20 or not name or len(name) > 200:
            entry["reason"] = "Course name or code violates catalogue limits"
            continue
        year_match = re.match(r"^(\d{4})(?:\s|$)", item["secili_mufredat"].strip())
        flag = {"zorunlu": False, "seçmeli": True}.get(normalized(item["ders_turu"]))
        try:
            credit, ects = number(item["kredi"]), number(item["akts"])
            semester = number(item["pozitif_yariyil"])
            if semester is not None and (semester <= 0 or int(semester) != semester):
                raise ValueError("Invalid semester")
        except ValueError as error:
            entry["reason"] = str(error)
            continue
        if not year_match or flag is None:
            entry["reason"] = "Unknown curriculum year or requirement type"
            continue
        proposed = {"curriculumKey": ck, "programmeKey": pk,
                    "catalogueYear": year_match.group(1), "semester": semester,
                    "recommendedStudyYear": None, "isElective": flag,
                    "courseCredit": credit, "ects": ects}
        if ck in curricula:
            old = curricula[ck]
            if old["programmeKey"] != pk:
                entry["reason"] = "Existing curriculum key belongs to another programme"
                continue
            old_course = next(c for c in result["courses"] if c["courseKey"] == old["courseKey"])
            if (old_course["departmentKey"], old_course["courseCode"].strip().upper()) != identity:
                entry["reason"] = "Existing curriculum key refers to another course identity"
                continue
            entry.update(status="preserved_existing", courseKey=old["courseKey"],
                         proposedMetadata=proposed,
                         differingFields=[k for k in proposed if old.get(k) != proposed[k]])
            if old_course["courseName"] != name:
                entry["courseNameDifference"] = {"existing": old_course["courseName"], "source": name}
            continue
        course = courses.get(identity)
        if len(names[identity]) > 1 or (course and normalized(course["courseName"]) != normalized(name)):
            entry["reason"] = "Conflicting names for department/course-code identity"
            continue
        if not course:
            course = {"courseKey": stable_course(*identity), "departmentKey": identity[0],
                      "courseCode": code, "courseName": name}
            if any(c["courseKey"] == course["courseKey"] for c in result["courses"]):
                raise ValueError("Generated course key collision")
            courses[identity] = course
            result["courses"].append(course)
        proposed["courseKey"] = course["courseKey"]
        result["programmeCourses"].append(proposed)
        curricula[ck] = proposed
        entry.update(status="included", programmeKey=pk, courseKey=course["courseKey"])

    before = collections.Counter(c["programmeKey"] for c in base["programmeCourses"])
    after = collections.Counter(c["programmeKey"] for c in result["programmeCourses"])
    report = {"purpose": "File-only candidate; no database application or automatic updates",
              "statusCounts": dict(collections.Counter(r["status"] for r in audit)),
              "pendingReasons": dict(collections.Counter(r.get("reason") for r in audit if r["status"] == "pending")),
              "addedCourses": len(result["courses"]) - len(base["courses"]),
              "addedCurriculumRows": len(result["programmeCourses"]) - len(base["programmeCourses"]),
              "coverage": [{"programmeKey": pk, "programmeName": p["programmeName"],
                            "before": before[pk], "after": after[pk]} for pk, p in programmes.items()],
              "rows": audit}
    # Baseline values and order must survive without changes, including explicit false/null.
    for collection in ("faculties", "departments", "programmes", "instructors", "courses", "programmeCourses"):
        assert result[collection][:len(base[collection])] == base[collection]
    assert len(audit) == len(source)
    return result, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--existing-curriculum", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    if args.output_dir.exists():
        raise ValueError("Output directory already exists; choose a new run directory")
    base = json.loads(args.baseline.read_text(encoding="utf-8-sig"))
    if base.get("schemaVersion") != 2:
        raise ValueError("Baseline schemaVersion must be 2")
    check_export(base, rows(args.existing_curriculum))
    source_files = sorted(args.source.glob("*.csv")) + [args.source / "OKU.md"]
    programme_source = rows(args.source / "01_Programlar.csv")
    source = rows(args.source / "02_Dersler_ve_Yariyillar.csv")
    result, report = prepare(base, programme_source, source)
    report["files"] = [{"path": str(p), "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
                       for p in [args.baseline, args.existing_curriculum, *source_files]]
    report["archiveCounts"] = {p.name: len(rows(p)) for p in args.source.glob("*.csv")}
    args.output_dir.mkdir(parents=True)
    for name, value in (("catalog.myo.candidate.json", result), ("myo-import-report.json", report)):
        with (args.output_dir / name).open("x", encoding="utf-8", newline="\n") as stream:
            json.dump(value, stream, ensure_ascii=False, indent=2, allow_nan=False)
            stream.write("\n")
    print(json.dumps({k: report[k] for k in ("statusCounts", "pendingReasons", "addedCourses", "addedCurriculumRows")}, ensure_ascii=True))


if __name__ == "__main__":
    main()
