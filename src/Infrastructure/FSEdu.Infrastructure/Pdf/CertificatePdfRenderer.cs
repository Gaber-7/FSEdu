using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FSEdu.Infrastructure.Pdf;

public sealed class CertificatePdfRenderer : ICertificatePdfRenderer
{
    static CertificatePdfRenderer()
    {
        // QuestPDF Community license — free for organizations <$1M annual revenue.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(CourseCertificateDto c)
    {
        var primary = "#5B4FE5";
        var secondary = "#F59E0B";
        var ink = "#0F172A";
        var muted = "#64748B";

        var doc = Document.Create(d =>
        {
            d.Page(p =>
            {
                p.Size(PageSizes.A4.Landscape());
                p.Margin(0);
                p.PageColor(Colors.White);
                p.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(14).FontColor(ink));

                p.Content().Column(col =>
                {
                    // Top accent strip
                    col.Item().Height(14).Background(primary);

                    // Body padding
                    col.Item().Padding(40).Column(body =>
                    {
                        body.Item().Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text("FSEdu")
                                .FontSize(28).FontColor(primary).Bold();
                            row.RelativeItem().AlignRight().Text($"Cert #: {c.CertificateNumber}")
                                .FontSize(11).FontColor(muted);
                        });

                        body.Item().PaddingTop(20).AlignCenter().Text("شَهَادَة إِتْمَامِ دَوْرَة")
                            .FontSize(34).Bold().FontColor(ink);
                        body.Item().AlignCenter().Text("Certificate of Course Completion")
                            .FontSize(13).FontColor(muted);

                        body.Item().PaddingTop(28).AlignCenter().Text("هذه الشهادة تُمنح إلى")
                            .FontSize(14).FontColor(muted);

                        body.Item().PaddingTop(8).AlignCenter().Text(c.StudentName)
                            .FontSize(36).Bold().FontColor(primary);

                        body.Item().PaddingTop(18).AlignCenter().Text("لإكمال دورة")
                            .FontSize(13).FontColor(muted);

                        body.Item().PaddingTop(6).AlignCenter().Text(c.CourseTitle)
                            .FontSize(22).SemiBold().FontColor(ink);

                        body.Item().PaddingTop(8).AlignCenter().Text(t =>
                        {
                            t.Span(c.SubjectName).FontColor(muted);
                            t.Span("  •  ").FontColor(muted);
                            t.Span(c.StageName).FontColor(muted);
                            t.Span("  •  ").FontColor(muted);
                            t.Span($"{c.TotalLessons} دروس").FontColor(muted);
                        });

                        // Bottom row: details
                        body.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("تاريخ الإصدار").FontSize(11).FontColor(muted);
                                left.Item().Text(c.CompletedAtUtc.ToString("yyyy/MM/dd"))
                                    .FontSize(15).SemiBold();
                            });
                            row.RelativeItem().AlignCenter().Column(mid =>
                            {
                                mid.Item().AlignCenter().Text("✦").FontColor(secondary).FontSize(28);
                                mid.Item().AlignCenter().Text("FullScreen Educational Solutions")
                                    .FontSize(11).FontColor(muted);
                            });
                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().AlignRight().Text("المُدرّس").FontSize(11).FontColor(muted);
                                right.Item().AlignRight().Text($"أ/ {c.TeacherName}")
                                    .FontSize(15).SemiBold();
                            });
                        });
                    });

                    // Bottom accent strip
                    col.Item().Height(14).Background(secondary);
                });
            });
        });

        return doc.GeneratePdf();
    }
}
