namespace EFDataAccess.Models
{
	public class EFAppTransfer
	{
        public int SourceAppTrxId { get; set; }
		public int DestinationAppTrxId { get; set; }

		public virtual Spend SourceAppTrx { get; set; }
		public virtual Spend DestinationAppTrx { get; set; }
	}
}
