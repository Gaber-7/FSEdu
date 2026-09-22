using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

public sealed class Chapter : Entity<Guid>
{
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public int OrderNum { get; private set; }

    private readonly List<Lesson> _lessons = new();
    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();

    private Chapter() { }

    public Chapter(Guid id, Guid courseId, string title, int order) : base(id)
    {
        CourseId = courseId;
        Title = title;
        OrderNum = order;
    }

    public void AddLesson(Lesson lesson) => _lessons.Add(lesson);
    public void Rename(string title) => Title = title;
    public void Reorder(int order) => OrderNum = order;
}
