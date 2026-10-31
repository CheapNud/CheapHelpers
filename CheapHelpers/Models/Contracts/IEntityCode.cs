namespace CheapHelpers.Models.Contracts
{
    public interface IEntityCode : IEntityId
    {
        public string Code { get; set; }
    }
}
