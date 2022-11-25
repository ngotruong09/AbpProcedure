using System.Collections.Generic;

namespace AbpProcedure.Base
{
    /// <summary>
    /// Represents a item with guid key.
    /// </summary>
    public class ObjectReturn
    {
        public string Id { get; }
        public ObjectReturn()
        {
            Id = System.Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Represents a item without key.
    /// </summary>
    public class ObjectReturnWithoutKey
    {
        public ObjectReturnWithoutKey()
        {
        }
    }
}