namespace Portalum.Zvt.Responses
{
    public interface IResponseDate
    {
        /// <summary>
        /// The month of the date.
        /// </summary>
        public int DateMonth { get; set; }

        /// <summary>
        /// The day of the date.
        /// </summary>
        public int DateDay { get; set; }
    }
}
