import argparse
import json
import math
import sys
from pathlib import Path

from openpyxl import load_workbook
from catalog_staging import extend_catalog


def blank(value):
    return value is None or (
        isinstance(value, str) and not value.strip()
    )


def text(value, required=False, maximum=None):
    if blank(value):
        if required:
            raise ValueError("Zorunlu metin alanı boş.")
        return None

    if isinstance(value, bool):
        raise ValueError("Metin alanında boolean değer bulundu.")

    # Excel'deki sayısal kaynak anahtarlarını metne dönüştürür.
    if isinstance(value, float) and value.is_integer():
        value = int(value)

    result = str(value).strip()

    if maximum is not None and len(result) > maximum:
        raise ValueError(f"Metin uzunluğu {maximum} sınırını aşıyor.")

    return result


def key(value):
    return text(value, required=True, maximum=100).upper()


def number(value, integer=False):
    if blank(value):
        return None

    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError("Sayısal alan Excel'de sayı olmalı.")

    if not math.isfinite(value):
        raise ValueError("Geçersiz sayısal değer.")

    if integer:
        if value < 1 or int(value) != value:
            raise ValueError("Pozitif tam sayı bekleniyor.")
        return int(value)

    if value < 0:
        raise ValueError("Kredi/AKTS negatif olamaz.")

    return value


def boolean(value):
    if blank(value):
        return None
    if isinstance(value, bool):
        return value
    raise ValueError("Seçmelilik Excel'de boolean veya boş olmalı.")


def read_rows(folder, filename, sheet_name, required_columns):
    path = folder / filename
    if not path.is_file():
        raise ValueError(f"Kaynak dosya bulunamadı: {filename}")

    workbook = load_workbook(
        path,
        read_only=True,
        data_only=False,
    )

    try:
        if sheet_name not in workbook.sheetnames:
            raise ValueError(f"{filename}: {sheet_name} sayfası yok.")

        sheet = workbook[sheet_name]
        iterator = sheet.iter_rows()
        header_cells = next(iterator, None)

        if header_cells is None:
            raise ValueError(f"{filename}/{sheet_name}: Sayfa boş.")

        headers = [
            str(cell.value).strip() if cell.value is not None else ""
            for cell in header_cells
        ]

        named_headers = [header for header in headers if header]
        if len(named_headers) != len(set(named_headers)):
            raise ValueError(
                f"{filename}/{sheet_name}: Tekrarlanan sütun adı."
            )

        missing = set(required_columns) - set(headers)
        if missing:
            raise ValueError(
                f"{filename}/{sheet_name}: Eksik sütunlar: "
                + ", ".join(sorted(missing))
            )

        result = []

        for row_number, cells in enumerate(iterator, start=2):
            if all(blank(cell.value) for cell in cells):
                continue

            record = {}

            for column, cell in zip(headers, cells):
                if column not in required_columns:
                    continue

                if cell.data_type in ("f", "e"):
                    raise ValueError(
                        f"{filename}/{sheet_name}, satır {row_number}, "
                        f"{column}: Formül veya Excel hatası var."
                    )

                record[column] = cell.value

            # Yalnız kaynak notu içeren satırları sessizce atlama.
            if all(blank(record.get(c)) for c in required_columns):
                raise ValueError(
                    f"{filename}/{sheet_name}, satır {row_number}: "
                    "Katalog alanları boş, başka alanlar dolu."
                )

            result.append((row_number, record))

        return result
    finally:
        workbook.close()


def convert_rows(folder, filename, sheet_name, mapping):
    """
    mapping:
        Excel sütunu -> (JSON alanı, dönüştürme fonksiyonu)
    """
    result = []

    for row_number, source in read_rows(
        folder, filename, sheet_name, mapping
    ):
        target = {}

        for source_field, (target_field, converter) in mapping.items():
            try:
                target[target_field] = converter(source[source_field])
            except ValueError as error:
                raise ValueError(
                    f"{filename}/{sheet_name}, satır {row_number}, "
                    f"{source_field}: {error}"
                ) from error

        result.append(target)

    return result


def required_name(value):
    return text(value, required=True, maximum=200)


def optional_100(value):
    return text(value, maximum=100)


def unique_index(rows, field, collection):
    result = {}

    for row in rows:
        value = row[field]
        if value in result:
            raise ValueError(
                f"{collection}: Tekrarlanan anahtar: {value}"
            )
        result[value] = row

    return result


def check_reference(rows, field, parents, collection):
    for row in rows:
        if row[field] not in parents:
            raise ValueError(
                f"{collection}: Çözülemeyen {field}: {row[field]}"
            )


def build_catalog(folder):
    faculties = convert_rows(
        folder, "Faculty.xlsx", "Faculty",
        {
            "faculty_key": ("facultyKey", key),
            "faculty_name": ("facultyName", required_name),
        },
    )

    departments = convert_rows(
        folder, "Departments.xlsx", "Departments",
        {
            "department_key": ("departmentKey", key),
            "faculty_key": ("facultyKey", key),
            "department_name": ("departmentName", required_name),
            "department_code": (
                "departmentCode",
                lambda v: text(v, maximum=30),
            ),
        },
    )

    programmes = convert_rows(
        folder, "Programmes.xlsx", "Programmes",
        {
            "programme_key": ("programmeKey", key),
            "department_key": ("departmentKey", key),
            "programme_name": ("programmeName", required_name),
            "degree_level": ("degreeLevel", optional_100),
            "instruction_language": (
                "instructionLanguage", optional_100
            ),
            "bologna_programme_id": (
                "bolognaProgrammeId", optional_100
            ),
        },
    )

    instructors = convert_rows(
        folder, "Instructors.xlsx", "Instructors",
        {
            "instructor_key": ("instructorKey", key),
            "department_key": ("departmentKey", key),
            "full_name": (
                "fullName",
                lambda v: text(v, required=True, maximum=150),
            ),
            "academic_title": (
                "academicTitle",
                lambda v: text(v, maximum=50),
            ),
        },
    )

    # Geçici alanlar yalnız eşleştirme sırasında kullanılır.
    pilot_courses = convert_rows(
        folder, "Course_Pilot_Anestezi.xlsx", "Course",
        {
            "course_key": ("courseKey", key),
            "department_key": ("departmentKey", key),
            "course_code": (
                "courseCode",
                lambda v: text(v, required=True, maximum=20),
            ),
            "course_name": ("courseName", required_name),
            "course_credit": ("courseCredit", number),
            "ects": ("ects", number),
            "is_elective": ("isElective", boolean),
        },
    )

    pilot_index = unique_index(
        pilot_courses, "courseKey", "Pilot Course"
    )

    courses = [
        {
            "courseKey": row["courseKey"],
            "departmentKey": row["departmentKey"],
            "courseCode": row["courseCode"],
            "courseName": row["courseName"],
        }
        for row in pilot_courses
    ]

    curriculum = convert_rows(
        folder, "Course_Pilot_Anestezi.xlsx", "ProgrammeCourse",
        {
            "curriculum_key": ("curriculumKey", key),
            "programme_key": ("programmeKey", key),
            "course_key": ("courseKey", key),
            "catalogue_year": (
                "catalogueYear",
                lambda v: text(v, maximum=50),
            ),
            "semester": (
                "semester",
                lambda v: number(v, integer=True),
            ),
            "recommended_study_year": (
                "recommendedStudyYear",
                lambda v: number(v, integer=True),
            ),
            "requirement_type": (
                "_requirementType",
                lambda v: text(v),
            ),
        },
    )

    for row in curriculum:
        source_course = pilot_index.get(row["courseKey"])
        if source_course is None:
            raise ValueError(
                "ProgrammeCourse için Course bulunamadı: "
                + row["courseKey"]
            )

        for field in ("courseCredit", "ects", "isElective"):
            row[field] = source_course[field]

        # Kaynak açıklamasını JSON'a ekleme; çelişki varsa dur.
        requirement = row.pop("_requirementType")
        if requirement is not None:
            expected = {
                "zorunlu": False,
                "seçmeli": True,
            }.get(requirement.casefold())

            if expected is None:
                raise ValueError(
                    "Bilinmeyen requirement_type: "
                    + row["curriculumKey"]
                )

            if row["isElective"] is not expected:
                raise ValueError(
                    "requirement_type ve is_elective çelişiyor: "
                    + row["curriculumKey"]
                )

    catalog = {
        "schemaVersion": 2,
        "faculties": faculties,
        "departments": departments,
        "instructors": instructors,
        "programmes": programmes,
        "courses": courses,
        "programmeCourses": curriculum,
    }

    indexes = {
        "faculties": unique_index(
            faculties, "facultyKey", "faculties"
        ),
        "departments": unique_index(
            departments, "departmentKey", "departments"
        ),
        "instructors": unique_index(
            instructors, "instructorKey", "instructors"
        ),
        "programmes": unique_index(
            programmes, "programmeKey", "programmes"
        ),
        "courses": unique_index(
            courses, "courseKey", "courses"
        ),
        "programmeCourses": unique_index(
            curriculum, "curriculumKey", "programmeCourses"
        ),
    }

    check_reference(
        departments, "facultyKey", indexes["faculties"], "departments"
    )

    for collection in ("instructors", "programmes", "courses"):
        check_reference(
            catalog[collection],
            "departmentKey",
            indexes["departments"],
            collection,
        )

    check_reference(
        curriculum, "programmeKey", indexes["programmes"],
        "programmeCourses",
    )
    check_reference(
        curriculum, "courseKey", indexes["courses"],
        "programmeCourses",
    )

    # Pilot kapsamı: dersin bölüm bağlamı doğrulanmış programa dayanır.
    for row in curriculum:
        course = indexes["courses"][row["courseKey"]]
        programme = indexes["programmes"][row["programmeKey"]]

        if course["departmentKey"] != programme["departmentKey"]:
            raise ValueError(
                "Pilot program/bölüm eşleşmesi uyuşmuyor: "
                + row["curriculumKey"]
            )

    used_courses = {row["courseKey"] for row in curriculum}
    if used_courses != set(pilot_index):
        raise ValueError(
            "Pilot Course ve ProgrammeCourse anahtar kümeleri farklı."
        )

    identities = set()
    for course in courses:
        identity = (
            course["departmentKey"],
            course["courseCode"].upper(),
        )
        if identity in identities:
            raise ValueError(
                "Aynı bölümde tekrarlanan ders kodu: "
                + course["courseCode"]
            )
        identities.add(identity)

    # Bu Datas2 pilotunda doğrulanmış özel değer.
    ans107 = indexes["programmeCourses"].get("CUR-PRG0002-ANS107")
    if ans107 is None or ans107["isElective"] is not False:
        raise ValueError("ANS107 kaydı bulunmalı ve isElective=false olmalı.")

    return catalog


def main():
    parser = argparse.ArgumentParser(
        description="Datas2 Excel paketini CatalogImport v2 JSON'a dönüştürür."
    )
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--staging-report", required=True, type=Path,
                        help="All staging rows and explicit inclusion/pending reasons; not an import file")
    parser.add_argument("--eem-year", choices=['2025', '2026'],
                        help="Use only after the team resolves the EEM source-year conflict")
    args = parser.parse_args()

    source = args.source.resolve()
    output = args.output.resolve()
    report_path = args.staging_report.resolve()
    if report_path == output or report_path.exists():
        raise ValueError("Staging report must be a new, separate file.")

    if not source.is_dir():
        raise ValueError("Kaynak klasör bulunamadı.")

    if output.suffix.lower() != ".json":
        raise ValueError("Çıktı uzantısı .json olmalı.")

    if output.exists():
        raise ValueError(
            "Çıktı zaten mevcut; üzerine yazılmadı. Yeni bir ad seç."
        )

    catalog = build_catalog(source)
    report = extend_catalog(source, catalog, read_rows, text, number, args.eem_year)

    # Önce bellekte serileştir; hata varsa dosya oluşturulmaz.
    payload = json.dumps(
        catalog,
        ensure_ascii=False,
        indent=2,
        allow_nan=False,
    ) + "\n"

    output.parent.mkdir(parents=True, exist_ok=True)
    report_payload = json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False) + "\n"
    report_path.parent.mkdir(parents=True, exist_ok=True)
    with report_path.open('x', encoding='utf-8', newline='\n') as handle:
        handle.write(report_payload)

    # 'x' modu mevcut dosyanın üzerine yazılmasını engeller.
    with output.open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(payload)

    print(f"JSON oluşturuldu: {output}")
    for collection, rows in catalog.items():
        if isinstance(rows, list):
            print(f"{collection}: {len(rows)}")

    print("Veritabanı bağlantısı veya aktarımı yapılmadı.")
    print(f"Staging: {report['counts']}")
    print("Sonraki adım: Uygulamanın validate-catalog komutunu çalıştır.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError) as error:
        print(f"HATA: {error}", file=sys.stderr)
        sys.exit(1)
