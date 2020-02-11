using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Model;

namespace church.ccv.CCVCore.HubSpot.Model
{
    [Table( "_church_ccv_HubSpot_HubSpotContact" )]
    [DataContract]
    public class HubSpotContact : Entity<HubSpotContact>, IRockEntity
    {
        [DataMember]
        public int PersonAliasId { get; set; }

        [DataMember]
        public int HubSpotObjectId { get; set; }

        [DataMember]
        public DateTime LastSyncDateTime { get; set; }

        [DataMember]
        public DateTime CreatedDateTime { get; set; }

        [DataMember]
        public DateTime ModifiedDateTime { get; set; }

        [NotMapped]
        public string FirstName { get; set; }

        [NotMapped]
        public string LastName { get; set; }

        [NotMapped]
        public string Email { get; set; }

        public virtual PersonAlias PersonAlias { get; set; }
    }

    public class HubSpotContactConfiguration : EntityTypeConfiguration<HubSpotContact>
    {
        public HubSpotContactConfiguration()
        {
            this.HasEntitySetName( "HubSpotContact" );

            this.HasRequired( a => a.PersonAlias ).WithMany().HasForeignKey( a => a.PersonAliasId ).WillCascadeOnDelete( false );
        }
    }
}
