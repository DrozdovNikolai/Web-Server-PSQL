namespace SuperHeroAPI.Models
{
    public class TableCreationRequest
    {
        public string TableName { get; set; }
        public List<ColumnDefinition> Columns { get; set; }
    }
}
