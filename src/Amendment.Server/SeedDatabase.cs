using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.Enums;
using Amendment.Repository.Infrastructure;
using Amendment.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Amendment.Server
{
    public class SeedDatabase
    {
        private readonly IServiceProvider _serviceProvider;

        public SeedDatabase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Seed()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var amendmentRepository = scope.ServiceProvider.GetRequiredService<IRepository<Model.DataModel.Amendment>>();

                await SeedSampleAmendment(unitOfWork, amendmentRepository);
                //await unitOfWork.SaveChangesAsync();
            }
        }

        private async Task SeedSampleAmendment(IUnitOfWork unitOfWork, IRepository<Model.DataModel.Amendment> amendmentRepository)
        {
            var dbCount = await amendmentRepository.CountAsync();
            if (dbCount > 0)
                return;

            amendmentRepository.Add(new Model.DataModel.Amendment()
            {
                Title = "Test Amendment 1",
                Author = "Some Author",
                Motion = "WC-1",
                LegisId = "Legid 2",
                PrimaryLanguageId = 1,
                Source = "Conference Floor",
                EnteredBy = -1,
                EnteredDate = DateTime.UtcNow,
                LastUpdatedBy = -1,
                LastUpdated = DateTime.UtcNow,
                AmendmentBodies = new List<AmendmentBody>()
                {
                    new AmendmentBody(){ AmendBody = "Just look at all this text. It boggles the mind.", LanguageId = 1, AmendStatus = AmendmentBodyStatus.Ready}
                }
                , IsLive = false
            });

            amendmentRepository.Add(new Model.DataModel.Amendment()
            {
                Title = "Test Amendment 2",
                Author = "Another Author",
                Motion = "WC-2",
                LegisId = "Legid 3",
                PrimaryLanguageId = 1,
                Source = "Conference Floor",
                EnteredBy = -1,
                EnteredDate = DateTime.UtcNow,
                LastUpdatedBy = -1,
                LastUpdated = DateTime.UtcNow,
                AmendmentBodies = new List<AmendmentBody>()
                {
                    new AmendmentBody(){ AmendBody = @$"### Substitución de la Resolución G-1

{LoremNET.Lorem.Paragraph(6, 9)}
**NEWSLIDE**
{LoremNET.Lorem.Paragraph(6, 7)}
**NEWSLIDE**
{LoremNET.Lorem.Paragraph(6, 8)}", LanguageId = 2, IsLive = true, AmendStatus = AmendmentBodyStatus.Ready },
                    new AmendmentBody(){ AmendBody = @$"### Substitute Resolution for G-1

{LoremNET.Lorem.Paragraph(6, 9)}
**NEWSLIDE**
{LoremNET.Lorem.Paragraph(6, 7)}
**NEWSLIDE**
{LoremNET.Lorem.Paragraph(6, 8)}", LanguageId = 1, IsLive = true, AmendStatus = AmendmentBodyStatus.Ready },
                    new AmendmentBody(){ AmendBody = @$"### Substitution de la résolution G-1

{LoremNET.Lorem.Paragraph(6, 9)}
**NEWSLIDE**
{LoremNET.Lorem.Paragraph(6, 7)}
**NEWSLIDE**
{LoremNET.Lorem.Paragraph(6, 8)}", LanguageId = 3, IsLive = true, AmendStatus = AmendmentBodyStatus.Ready }
                },
                IsLive = true
            });

            for (int i = 4; i < 10; i++)
            {
                amendmentRepository.Add(Model.DataModel.Amendment.GenerateNew(i));
            }

            await unitOfWork.SaveChangesAsync(-999);
        }
    }
}