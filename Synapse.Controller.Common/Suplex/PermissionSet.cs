using System;

using Suplex.Security.AclModel;

namespace Synapse.Services.Controller.Dal
{
    public enum RecordState
    {
        Unchanged,
        Added,
        Modified,
        Deleted
    };

    public enum PermissionRole
    {
        None,
        Reader,
        Writer,
        Admin
    };

    public class PermissionSet
    {
        public int Id { get; set; }                     // Primary Key from Suplex Table
        public Guid GroupUId { get; set; }               // Unique Id Of Container Security Group
        public string GroupName { get; set; }           // Name of Container Security Group
        public FileSystemRight Rights { get; set; }     // Group Rights to the Container
        public RecordState State { get; set; }          // Current State of the Record
    }
}