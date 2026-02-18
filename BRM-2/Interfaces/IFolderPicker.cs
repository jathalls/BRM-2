using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRM_2.Interfaces
{
    public interface IFolderPicker
    {
        Task<string> PickFolderAsync();
    }
}

