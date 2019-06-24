﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YourShares.Application.SearchModels
{
    public class UserSearchModel: BaseSearchModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
