﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WindowsServerDnsUpdater.Data;

#nullable disable

namespace WindowsServerDnsUpdater.Migrations
{
    [DbContext(typeof(LoggingDbContext))]
    partial class LoggingDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("WindowsServerDnsUpdater.Models.LogRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Exception")
                        .HasColumnType("TEXT");

                    b.Property<string>("Level")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Logger")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("LogEntries");
                });

            modelBuilder.Entity("WindowsServerDnsUpdater.Models.Settings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CacheUpdateIntervalSeconds")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DefaultDomain")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("LeaseUpdateDelaySeconds")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MikrotikIp")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MikrotikLogin")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MikrotikPassword")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("VpnSitesListName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("VpnSitesListUpdateDelaySeconds")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Settings");
                });
#pragma warning restore 612, 618
        }
    }
}
