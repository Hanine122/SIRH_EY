using System;
using System.Threading.Tasks;

namespace SIRH.EY.Services
{
    public interface IParametreService
    {
        T GetValue<T>(string code, T defaultValue = default);
        void SetValue(string code, string valeur);
    }
}