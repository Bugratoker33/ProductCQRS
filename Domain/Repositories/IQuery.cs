using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories;

public interface IQuery<T>
{
    IQueryable<T> Query();//linqu sorgusu 
}
