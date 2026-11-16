"""Convert verified staging rows; retain every source row and explicit blockers."""
import hashlib
import json
import re


def clean(value):
    if value is None or str(value).strip().casefold() in ('', 'not publicly published'):
        return None
    return str(value).strip()


def stable(prefix, *parts):
    # Identity is independent of Excel row order and display names.
    raw = json.dumps(parts, ensure_ascii=False, separators=(',', ':'))
    return prefix + hashlib.sha256(raw.encode('utf-8')).hexdigest()[:32].upper()


def extend_catalog(folder, catalog, read_rows, text, number, eem_year):
    columns = ('source_row programme_name bologna_programme_id programme_key_candidate '
               'programme_mapping_status catalogue_year semester bologna_course_id course_code '
               'course_name hours_t_u_l requirement_type ects group_course_count instruction_mode '
               'instruction_language responsible_department_key responsible_department_status '
               'pending_reason programme_source_url verified_bologna_programme_id '
               'verified_catalogue_year official_course_id official_course_url').split()
    source = read_rows(folder, 'Course_Staging.xlsx', 'Course staging', columns)
    evidence_columns = ('source_row source_course_code programme_key department_key official_curriculum_year '
                        'official_course_code official_course_name official_course_id').split()
    evidence = {}
    for _, row in read_rows(folder, 'Catalogue_Mapping_Evidence.xlsx', 'Business_4190_2025', evidence_columns):
        if row['source_row'] in evidence:
            raise ValueError('Duplicate business evidence source_row')
        evidence[row['source_row']] = row
    programmes = {p['programmeKey']: p for p in catalog['programmes']}
    courses = {(c['departmentKey'], c['courseCode'].upper()): c for c in catalog['courses']}
    curricula = {c['curriculumKey']: c for c in catalog['programmeCourses']}
    report = {'schemaVersion': 1, 'purpose': 'Source archive; NOT an import-catalog file', 'rows': []}
    for excel_row, row in source:
        item = {'excelRow': excel_row, 'source': row, 'status': 'pending', 'reason': None}
        report['rows'].append(item)
        if 'excluded elective group heading' in str(row['programme_mapping_status']).lower():
            item.update(status='excluded', reason='Elective group heading, not a course')
            continue
        pk, dk = clean(row['programme_key_candidate']), clean(row['responsible_department_key'])
        if not str(row['programme_mapping_status']).startswith('Verified') or not dk:
            item['reason'] = 'Programme/department source mapping not verified'
            continue
        if pk not in programmes or programmes[pk]['departmentKey'] != dk:
            raise ValueError(f'Staging row {excel_row}: invalid programme/department context')
        if pk == 'PRG-0019' and eem_year is None:
            item['reason'] = 'EEM year conflict: workbook 2026 versus team message 2025'
            continue
        code, name = clean(row['course_code']), clean(row['course_name'])
        year = clean(row['verified_catalogue_year'])
        if pk == 'PRG-0050':
            ev = evidence.get(row['source_row'])
            if not ev or ev['programme_key'] != pk or ev['department_key'] != dk:
                raise ValueError(f'Staging row {excel_row}: missing or conflicting evidence')
            if clean(ev['source_course_code']) != code:
                raise ValueError(f'Staging row {excel_row}: evidence code differs')
            code = clean(ev['official_course_code'])
            name = clean(ev['official_course_name'])
            year = clean(ev['official_curriculum_year'])
        elif pk == 'PRG-0019':
            # Preserve each source occurrence's year; the option acknowledges
            # the current packet, never rewrites all years to one value.
            year_source = year or clean(row['catalogue_year'])
            match = re.match(r'^(\d{4})(?:\s|$)', year_source or '')
            if not match:
                item['reason'] = 'EEM curriculum year is missing or ambiguous'
                continue
            year = match.group(1)
        else:
            item['reason'] = 'New verified programme needs an explicit evidence mapping rule'
            continue
        code = text(code, required=True, maximum=20)
        name = text(name, required=True, maximum=200)
        identity = (dk, code.upper())
        course = courses.get(identity)
        if course and course['courseName'] != name:
            item['reason'] = 'Conflicting course name for existing department/code'
            continue
        semester = clean(row['semester'])
        semester = number(int(semester), integer=True) if semester else None
        requirement = clean(row['requirement_type'])
        flags = {'zorunlu': False, 'seçmeli': True}
        if requirement and requirement.casefold() not in flags:
            raise ValueError(f'Staging row {excel_row}: unknown requirement type')
        if not course:
            course = {'courseKey': stable('CRS-', dk, code.upper()), 'departmentKey': dk,
                      'courseCode': code, 'courseName': name}
        ck = stable('CUR-', pk, course['courseKey'], year, semester)
        curriculum = {'curriculumKey': ck, 'programmeKey': pk, 'courseKey': course['courseKey'],
                      'catalogueYear': year, 'semester': semester, 'recommendedStudyYear': None,
                      'courseCredit': None, 'ects': number(row['ects']),
                      'isElective': flags[requirement.casefold()] if requirement else None}
        if ck in curricula and curricula[ck] != curriculum:
            raise ValueError(f'Staging row {excel_row}: conflicting curriculum occurrence')
        if identity not in courses:
            courses[identity] = course
            catalog['courses'].append(course)
        if ck not in curricula:
            curricula[ck] = curriculum
            catalog['programmeCourses'].append(curriculum)
        item.update(status='included', reason=None, courseKey=course['courseKey'], curriculumKey=ck)
    report['counts'] = {s: sum(r['status'] == s for r in report['rows']) for s in ('included', 'pending', 'excluded')}
    return report
