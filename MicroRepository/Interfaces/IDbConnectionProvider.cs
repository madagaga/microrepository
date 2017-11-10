using MicroRepository.Templates;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MicroRepository.Interfaces
{
    public interface IDbConnectionProvider
    {
        SqlTemplate Template {get;set;}

        string ProviderName { get;  }        
    }
}
