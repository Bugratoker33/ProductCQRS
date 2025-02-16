

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Domain.Repositories;

public class Entitiy<TId>: IEntityTimestamps
{
    public TId Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeletedDate { get; set; }

    public Entitiy()
    {
        Id = default;

    }
    public Entitiy(TId id)
    {
        Id = id;
    }
}
