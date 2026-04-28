using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIRH.EY.Services
{
    public interface IDataverseService
    {
        Task<List<Entity>> GetCollaborateursAsync();
        Task<Entity> GetCollaborateurByIdAsync(Guid id);
        Task<Guid> CreateCollaborateurAsync(Entity collaborateur);
        Task UpdateCollaborateurAsync(Entity collaborateur);
        Task DeleteCollaborateurAsync(Guid id);
    }
}