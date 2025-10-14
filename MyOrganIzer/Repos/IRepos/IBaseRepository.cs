using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganIzer.Repos.IRepos
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {
    }
}
