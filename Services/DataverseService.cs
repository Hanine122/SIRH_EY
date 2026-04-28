using Data8.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIRH.EY.Services
{
    public class DataverseService : IDataverseService
    {
        private readonly IOrganizationService _serviceClient;

        public DataverseService(string environmentUrl, string username, string password)
        {
            var organizationServiceUrl = $"{environmentUrl}/XRMServices/2011/Organization.svc";
            _serviceClient = new OnPremiseClient(organizationServiceUrl, username, password);
        }

        public async Task<List<Entity>> GetCollaborateursAsync()
        {
            var query = new QueryExpression("contact") // adaptez "contact" si votre table s'appelle autrement
            {
                ColumnSet = new ColumnSet("fullname", "emailaddress1", "jobtitle", "parentcustomerid", "statuscode")
            };
            var result = await Task.Run(() => _serviceClient.RetrieveMultiple(query));
            return result.Entities.ToList();
        }

        public async Task<Entity> GetCollaborateurByIdAsync(Guid id)
        {
            return await Task.Run(() => _serviceClient.Retrieve("contact", id, new ColumnSet(true)));
        }

        public async Task<Guid> CreateCollaborateurAsync(Entity collaborateur)
        {
            return await Task.Run(() => _serviceClient.Create(collaborateur));
        }

        public async Task UpdateCollaborateurAsync(Entity collaborateur)
        {
            await Task.Run(() => _serviceClient.Update(collaborateur));
        }

        public async Task DeleteCollaborateurAsync(Guid id)
        {
            await Task.Run(() => _serviceClient.Delete("contact", id));
        }
    }
}