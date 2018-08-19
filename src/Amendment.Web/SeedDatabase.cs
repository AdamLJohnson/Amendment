using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository.Infrastructure;
using Amendment.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Amendment.Web
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
            amendmentRepository.Add(new Model.DataModel.Amendment()
            {
                Id = 1,
                AmendTitle = "Test Amendment 1",
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
                    new AmendmentBody(){ AmendBody = "Just look at all this text. It boggles the mind.", LanguageId = 1 }
                }
            });

            amendmentRepository.Add(new Model.DataModel.Amendment()
            {
                Id = 2,
                AmendTitle = "Test Amendment 2",
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
                    new AmendmentBody(){ AmendBody = "This is the body of the amendment. It sure is neat", LanguageId = 1 }
                }
            });

            await unitOfWork.SaveChangesAsync();
        }
    }
}