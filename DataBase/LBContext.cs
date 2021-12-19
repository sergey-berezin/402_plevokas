using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace DataBase
{
    public partial class Item
    {
        public int ItemId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public byte[] Image { get; set; }
        public string Label { get; set; }
    }

    class LBContext : DbContext
    {
        public DbSet<Item> Items { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            _ = b.Entity<Item>().HasKey(x => x.ItemId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder b)
        {
            b.UseSqlite(@"Data Source=C:\Users\white\source\repos\FinalLab\DataBase\library.db");
        }
    }
}