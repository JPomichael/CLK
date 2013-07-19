﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CLK.Reflection
{
    public abstract partial class ReflectContext
    {
        // Locator
        public static ReflectContext Current { get; set; }
    }

    public abstract partial class ReflectContext : IReflectContext
    {
        // Properties
        //public SettingDictionary AppSettings { get; private set; }


        // Methods
        public abstract TEntity CreateEntity<TEntity>(string sectionName) where TEntity : class;

        public abstract TEntity CreateEntity<TEntity>(string sectionName, string entityName) where TEntity : class;

        public abstract IEnumerable<TEntity> CreateAllEntity<TEntity>(string sectionName) where TEntity : class;
    }
}