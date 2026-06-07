namespace INFP_Proj.Models
{
    public class WardStatViewModel
    {
        public int WardID { get; set; }
        public string WardName { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }

        public double OccupancyRate => MaxCapacity > 0 ? ((double)OccupiedBeds / MaxCapacity) * 100 : 0;
    }
}