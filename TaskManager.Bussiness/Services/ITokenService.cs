using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Services
{
    public interface ITokenService
    {
        string GenerateToken(ApplicationUser user,IList<string> tokens);
    }
}
