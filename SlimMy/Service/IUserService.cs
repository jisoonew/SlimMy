﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Service
{
    public interface IUserService
    {
        string GetUserId();
        void SetUserId(string userId);
    }
}
