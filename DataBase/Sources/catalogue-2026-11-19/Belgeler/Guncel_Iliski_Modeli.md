# Güncel modelin ilişki özeti

Kaynak: feature/democontrol @ 5b79729, ApplicationDbContextModelSnapshot. Bu çizim güncel kodun veri ilişkilerini belgeler; yeni mimari önerisi veya uygulanmış veritabanı şeması iddiası değildir. İlk PDF planlama çizimindeki eksik Programme/ProgrammeCourse katmanı ve isteğe bağlı yorum açılışı burada görünür.

```mermaid
erDiagram
    Faculties ||--o{ Departments : contains
    Departments ||--o{ Instructors : primary_department
    Departments ||--o{ Programmes : offers
    Departments ||--o{ Courses : owns
    Departments o|--o{ UserAccounts : selected_department
    Programmes o|--o{ UserAccounts : selected_programme
    Programmes ||--o{ ProgrammeCourses : curriculum
    Courses ||--o{ ProgrammeCourses : appears_in
    Courses ||--o{ CourseOfferings : offered_in
    CourseOfferings ||--o{ OfferingInstructors : assignments
    Instructors ||--o{ OfferingInstructors : teaches
    Courses ||--o{ CoursePrerequisites : requires
    Courses ||--o{ CoursePrerequisites : prerequisite_of
    UserAccounts ||--o{ Reviews : writes
    Courses ||--o{ Reviews : reviewed_course
    Instructors ||--o{ Reviews : reviewed_instructor
    CourseOfferings o|--o{ Reviews : optional_context
    OfferingInstructors o|--o{ Reviews : optional_assignment
```

OfferingId doluysa Review için bileşik FK'ler (OfferingId,CourseId) ve (OfferingId,InstructorId) de geçerlidir. Mermaid çizimi bu bileşik alan eşitliğini tek başına göstermez; ayrıntı model/sözlüktedir. ProgrammeCourses ve OfferingInstructors farklı ilişkilerdir. Resmî program kadro üyelikleri ayrı kanıt listesidir; dönemlik OfferingInstructors satırı diye sunulmaz. İsim benzerliği nedeniyle fakülte/derece/dil ilişkileri birleştirilmez.
