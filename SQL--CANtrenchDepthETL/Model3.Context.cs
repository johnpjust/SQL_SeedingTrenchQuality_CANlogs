﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Seeding2019ETL
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class ISU_Internal_CANEntities : DbContext
    {
        public ISU_Internal_CANEntities()
            : base("name=ISU_Internal_CANEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<log_meta_data> log_meta_data { get; set; }
        public virtual DbSet<can_raw_data_Seeding_2019> can_raw_data_Seeding_2019 { get; set; }
    }
}
