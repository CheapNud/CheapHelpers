using System.ComponentModel.DataAnnotations;

namespace CheapHelpers.Models.Entities
{
    // Example concrete implementation
    /// <summary>
    /// Generic file attachment implementation
    /// </summary>
    public class GenericFileAttachment : FileAttachment
    {
        /// <summary>
        /// Generic foreign key - can reference any entity
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// Type of entity this file is attached to
        /// </summary>
        [StringLength(50)]
        public string? EntityType { get; set; }
    }
}
