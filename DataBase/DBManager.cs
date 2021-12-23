using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataBase
{
    public class DBManager
    {
        private readonly LBContext db = new();

        public event Action DataChanged;

        public async Task AddAsync(Item item)
        {
            if (CheckItemPresence(item))
            {
                return;
            }

            _ = await db.Items.AddAsync(item);
            _ = await db.SaveChangesAsync();
            DataChanged?.Invoke();
        }

        public void Clear()
        {
            _ = db.Database.EnsureDeleted();
            _ = db.Database.EnsureCreated();
            DataChanged?.Invoke();
        }

        private bool CheckItemPresence(Item possibleDuplicate)
        {
            IQueryable<Item> query = db.Items.Where(x => (x.X == possibleDuplicate.X) && (x.Y == possibleDuplicate.Y) && (x.Length == possibleDuplicate.Length) && (x.Width == possibleDuplicate.Width));
            foreach (Item obj in query)
            {
                if (obj.Image.SequenceEqual(obj.Image))
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<string> GetClasses()
        {
            IEnumerable<string> classList;
            classList = db.Items.Select(x => x.Label).Distinct().Select(x => $"[{db.Items.Where(y => y.Label == x).Count()}] {x}");
            return classList;
        }

        public IEnumerable<byte[]> GetImages(string selectedLabel)
        {
            IEnumerable<byte[]> imageList;
            imageList = db.Items.Where(x => x.Label == selectedLabel).Select(x => x.Image);
            return imageList;
        }
    }
}
