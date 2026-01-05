using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InternFreelance.Helpers
{
    public static class CertificateGenerator
    {
        public static void Generate(
            string filePath,
            string studentName,
            string projectTitle,
            string smeName,
            int rating,
            DateTime issuedAt,
            string certificateCode
        )
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    page.Content().Column(col =>
                    {
                        col.Spacing(20);

                        col.Item().AlignCenter().Text("Certificate of Completion")
                            .FontSize(26)
                            .Bold();

                        col.Item().AlignCenter().Text("This certifies that")
                            .FontSize(14);

                        col.Item().AlignCenter().Text(studentName)
                            .FontSize(20)
                            .Bold();

                        col.Item().AlignCenter().Text("has successfully completed the project")
                            .FontSize(14);

                        col.Item().AlignCenter().Text(projectTitle)
                            .FontSize(18)
                            .SemiBold();

                        col.Item().AlignCenter().Text($"Supervised by: {smeName}")
                            .FontSize(13);

                        col.Item().AlignCenter().Text($"Rating: {rating} / 5")
                            .FontSize(14)
                            .Bold();

                        col.Item().AlignCenter().Text($"Issued on: {issuedAt:dd MMM yyyy}")
                            .FontSize(12);

                        col.Item().PaddingTop(40).AlignCenter()
                            .Text($"Certificate Code: {certificateCode}")
                            .FontSize(10)
                            .Italic();
                    });
                });
            })
            .GeneratePdf(filePath);
        }
    }
}
