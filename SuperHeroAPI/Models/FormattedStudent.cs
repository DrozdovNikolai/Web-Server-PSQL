using SuperHeroAPI.md2;

namespace SuperHeroAPI.Models
{
    public class FormattedStudent
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string GroupName { get; set; }
        public string GroupNumber { get; set; }
        public string Subgroup { get; set; }
        public string FormattedEnrolledDate { get; set; }
        public string EnrollmentOrder { get; set; }
        public string FormattedDateOfBirth { get; set; }
        public int Course { get; set; }
        public int GroupId { get; set; }
        // Add more properties based on your Student model

        public FormattedStudent(Student student)
        {
            // Map properties from Student to FormattedStudent
            StudentId = student.StudentId;
            FullName = $"{student.LastName} {student.FirstName} {student.Patronymic}";
            GroupName = $"{student.Group.GroupNumber}/{student.Subgroup}";
            GroupNumber = student.Group.GroupNumber;
            Subgroup = student.Subgroup;
            FormattedEnrolledDate = student.EnrolledDate?.ToString("dd/MM/yyyy");
            EnrollmentOrder = student.EnrollmentOrder;
            FormattedDateOfBirth = student.DateOfBirth?.ToString("dd/MM/yyyy");
            Course = student.Course.GetValueOrDefault();
            GroupId = student.GroupId.GetValueOrDefault();
            // Map more properties based on your Student model
        }
    }
}
