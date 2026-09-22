using FSEdu.Shared.Contracts.Courses;

namespace FSEdu.Application.Abstractions;

public interface ICertificatePdfRenderer
{
    // Renders the certificate as an A4 landscape PDF and returns the raw bytes.
    byte[] Render(CourseCertificateDto cert);
}
