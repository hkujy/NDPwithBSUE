// Update  14-June-2018
// yu jiang 
namespace IOPT
{
    /// <summary>
    /// this class created to on board, board and alight flow
    /// </summary>
    public class FlowClass
    {
        
        protected internal int LineID { get; private set; }
        protected internal double Board { get; set; }
        protected internal double Alight { get; set; }
        protected internal double Arrival { get; set; }
        protected internal double OnBoard { get; set; }
        protected internal void Ini()
        {
            Board = 0; Alight = 0; OnBoard = 0; Arrival = 0;
        }
        public FlowClass()
        {
            LineID = PARA.NULLINT;
            Ini();
        }
        public FlowClass(int _LineID)
        {
            LineID = _LineID;
            Ini();
        }
    }

}
