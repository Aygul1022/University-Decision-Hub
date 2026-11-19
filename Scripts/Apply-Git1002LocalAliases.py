"""Apply the user-approved temporary aliases to catalogue files, without DB access.

Official source codes and historical verification results remain unchanged.
Run from any directory; a second run is idempotent.
"""
import copy
import csv
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'DataBase/Sources/catalogue-2026-11-19'
PACKAGES = [ROOT / 'DataBase/Imports/catalog.ready.json', SOURCE / 'Veri/catalog.ready.json']
OLD_KEY = 'CRS-DOU-B9423D9526C039D81F082152'


def stable_key(prefix, fields):
    payload = json.dumps(fields, ensure_ascii=False, separators=(',', ':')).encode('utf-8')
    return f'{prefix}-DOU-{hashlib.sha256(payload).hexdigest()[:24].upper()}'


def read_csv(path):
    with path.open(encoding='utf-8-sig', newline='') as f:
        return list(csv.DictReader(f))


def write_csv(path, rows, fields=None):
    fields = fields or list(dict.fromkeys(k for row in rows for k in row))
    with path.open('w', encoding='utf-8-sig', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fields)
        writer.writeheader()
        writer.writerows({k: '' if v is None else v
                          for k, v in row.items()} for row in rows)


def main():
    data = json.loads(PACKAGES[0].read_text(encoding='utf-8-sig'))
    assert data == json.loads(PACKAGES[1].read_text(encoding='utf-8-sig')), 'Package copies differ'
    original = copy.deepcopy(data)
    rows = read_csv(SOURCE / 'Kaynak_Kanitlari/GIT1002_Bekleyen_Kaynak_Satirlari.csv')
    assert len(rows) == 2
    assert {(r['programme_key'], r['department_key'], r['ders_kodu'], r['ders_adi'],
             r['bologna_ders_id'], r['yariyil'], r['akts'], r['kredi']) for r in rows} == {
        ('PRG-0031', 'DEP-0076', 'GİT 1002', 'Reklamcılık Tarihi', '4404573', s, '5', '')
        for s in ('4', '8')}
    existing = next(c for c in data['courses'] if c['courseKey'] == OLD_KEY)
    assert existing['departmentKey'] == 'DEP-0076' and existing['courseName'] == 'Yaratıcı Fotoğraf'
    assert existing['courseCode'] in ('GİT 1002', 'GİT 1002-0')
    existing['courseCode'] = 'GİT 1002-0'
    # Identity derives from original source identity, never from the temporary suffix.
    new_key = stable_key('CRS', ['DEP-0076', 'bologna', '4404573'])
    new_course = dict(courseKey=new_key, departmentKey='DEP-0076',
                      courseCode='GİT 1002-1', courseName='Reklamcılık Tarihi')
    matches = [c for c in data['courses'] if c['courseKey'] == new_key]
    if matches:
        assert matches == [new_course]
    else:
        data['courses'].append(new_course)
    links = []
    for row in rows:
        key = stable_key('CUR', ['full-catalog', row['bologna_program_id'], row['mufredat'],
                                 row['kaynak_yariyil'], row['bologna_ders_id']])
        link = dict(curriculumKey=key, programmeKey='PRG-0031', courseKey=new_key,
                    catalogueYear='2026', semester=int(row['yariyil']), recommendedStudyYear=None,
                    courseCredit=None, ects=5, isElective=True)
        found = [c for c in data['programmeCourses'] if c['curriculumKey'] == key]
        if found:
            assert found == [link]
        else:
            data['programmeCourses'].append(link)
        links.append(link)
    assert len({(c['departmentKey'], c['courseCode'].strip().upper()) for c in data['courses']}) == len(data['courses'])
    for collection, key in [('courses', 'courseKey'), ('programmeCourses', 'curriculumKey')]:
        after = {c[key]: c for c in data[collection]}
        assert len(after) == len(data[collection])
        for before in original[collection]:
            expected = dict(before)
            if collection == 'courses' and before[key] == OLD_KEY:
                expected['courseCode'] = 'GİT 1002-0'
            assert after[before[key]] == expected, before[key]
    assert len(data['courses']) == 9347 and len(data['programmeCourses']) == 12593
    for collection in ('faculties', 'departments', 'instructors', 'programmes'):
        assert data[collection] == original[collection]
    encoded = json.dumps(data, ensure_ascii=False, indent=2) + '\n'
    for path in PACKAGES:
        path.write_text(encoded, encoding='utf-8', newline='\n')
    for collection, filename in [('courses', 'Dersler.csv'), ('programmeCourses', 'Mufredat.csv')]:
        old_fields = list(read_csv(SOURCE / 'Veri' / filename)[0])
        write_csv(SOURCE / 'Veri' / filename, data[collection], old_fields)
    mapping = [dict(officialCourseCode='GİT 1002', localCourseCode=course['courseCode'],
                    courseKey=course['courseKey'], courseName=course['courseName'],
                    bolognaCourseId=cid, departmentKey='DEP-0076', programmeKey='PRG-0031',
                    decision='USER_APPROVED_TEMPORARY_ALIAS')
               for course, cid in [(existing, '4404534'), (new_course, '4404573')]]
    write_csv(SOURCE / 'Belgeler/GIT1002_Yerel_Kod_Eslesmesi.csv', mapping)
    # Raw evidence retains the official code. Aliases live in the separate mapping.
    evidence_path = SOURCE / 'Kaynak_Kanitlari/Ders_Mufredat_Kaynak_Eslesmeleri.csv'
    evidence = read_csv(evidence_path)
    for row in evidence:
        row.pop('yerel_ders_kodu', None)
        row.pop('aktarim_durumu', None)
        if row['programme_key'] == 'PRG-0031' and row['course_code'] == 'GİT 1002':
            if row['bologna_course_id'] == '4404573':
                link = next(c for c in links if c['semester'] == int(row['semester']))
                row.update(course_key=new_key, curriculum_key=link['curriculumKey'])
    write_csv(evidence_path, evidence)
    issues_path = SOURCE / 'Belgeler/Acik_Konular_ve_Sorumlular.csv'
    issues = read_csv(issues_path)
    issue = next(r for r in issues if r['issue'] == 'GIT_1002')
    if issue['status'] != 'YEREL_DB_UYGULANDI_RESMI_KOD_TEYIDI_BEKLIYOR':
        issue.update(status='GECICI_YEREL_KOD_PAKETTE_DB_BEKLIYOR',
                 detail='Kullanici onayi: Yaratici Fotograf GİT 1002-0; Reklamcilik Tarihi GİT 1002-1. Resmi kod GİT 1002 korunur; 1 ders ve 2 mufredat baglantisi pakete eklendi.',
                 local_evidence='File validation recorded separately. Database update and new HTTP checks pending.')
    write_csv(issues_path, issues)
    report = dict(counts={k: len(v) for k, v in data.items() if isinstance(v, list)},
                  packageSha256=hashlib.sha256(PACKAGES[0].read_bytes()).hexdigest(),
                  aliases=mapping, newCurricula=links,
                  preservation='All prior rows and keys preserved; only the approved CourseCode changed.',
                  databaseWritesPerformedByThisScript=False, migrationCreated=False, messageSent=False,
                  workbook='Historical workbook unchanged; use updated JSON/CSV and decision note.')
    output = ROOT / 'Doc/Verification/git1002-local-aliases-2026-11-19'
    output.mkdir(parents=True, exist_ok=True)
    (output / 'File_Change_Report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
