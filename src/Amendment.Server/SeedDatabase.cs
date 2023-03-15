using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
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
                    new AmendmentBody(){ AmendBody = "Just look at all this text. It boggles the mind.", LanguageId = 1 }
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
                    new AmendmentBody(){ AmendBody = @"### Substitute Resolution for G-1 Issues of (Priesthood) Morality

Whereas, The intent of the motion G-1 was to increase meaningful and respectful dialog among members and priesthood about the morality of lifestyles and activities on the one hand, and the values lifted up in Doctrine and Covenants 164:6a-b; therefore be it
**NEWSLIDE**
Resolved, That the First Presidency explore options and opportunities for meaningful and respectful dialog among members and priesthood how individual, family and congregational activities, lifestyles and choices can be more closely and more faithfully aligned with the values and morals lifted up in the Enduring Principles and Doctrine and Covenants 164:6a-b. These discussions themselves should embody these values while we encourage each other and ourselves to live lives worthy of Zion and a priesthood of all believers; and be it further
**NEWSLIDE**
Resolved, That the First Presidency make these guidelines, options and opportunities for meaningful and respectful dialog available to congregational and Mission Center leadership no later than June 7, 2018.", LanguageId = 1, IsLive = true },
                    new AmendmentBody(){ AmendBody = @"### Substitución de la Resolución G-1 – Asuntos de moralidad del sacerdocio

Considerando, la intención de la moción G-1 era aumentar el dialogo significativo y respetuoso entre los miembros y el sacerdocio sobre la moralidad de estilos de vida y actividades, por una lado,  y los valores  expresados en  Doctrina y Pactos 164:6a-b; por lo tanto
**NEWSLIDE**
Sea resuelto que, la Primera Presidencia explore las opciones y oportunidades significativas y dialogo respetuoso entre los miembros y el sacerdocio como actividades individuales, familiares y congregacionales, los estilos y elecciones de vida puedan estar más alineadas fielmente con los valores y moralidad  expresados en los principios duraderos y Doctrina y Pactos 164:6 a-b. Estas discusiones en sí encarnan estos valores mientras nos animamos unos a otros a vivir vidas dignas de Sion y a un sacerdocio de todos los creyentes; y que además
**NEWSLIDE**
Sea resuelto que, la  Primera Presidencia haga estas guías, opciones y oportunidades para un dialogo sincero y respetuoso, disponible para el liderazgo congregacional y el centro de misión a no más tardar del 7 de Junio, 2018.", LanguageId = 2, IsLive = true },
                    new AmendmentBody(){ AmendBody = @"### Substitution de la résolution G-1 – Question de moralité (prêtrise) 

Attendu que, l’intention de la résolution G-1 est de développer encore plus un dialogue sincère et important pour les membres et la prêtrise au sujet de la moralité des modes de vies et activités d’une part, et des valeurs soulignées dans D&A 164:6 a-b; il est donc, 
**NEWSLIDE**
Résolu que, la Première Présidence explore des options et des opportunités d’un dialogue sincère et important pour les membres et la prêtrise sur la manière dont les activités, les modes de vie, et les choix des personnes, des familles et des congrégations, peuvent s’aligner plus fidèlement sur les valeurs et la morale soulignées dans les principes permanents et dans D&A164:6 a-b. Ces discussions doivent en soi incarner ces valeurs tandis que nous encourageons chacun et nous-mêmes à vivre une vie digne de Sion et une prêtrise de tous les croyants. Il est également
**NEWSLIDE**
Résolu que, la Première Présidence crée ces directives, options et opportunités pour un dialogue important et respectueux accessible aux dirigeants de congrégations et de centres de mission au plus tard le 7 juin 2018.", LanguageId = 3, IsLive = true }
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