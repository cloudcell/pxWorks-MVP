namespace uGraph
{
    public interface IAcceptor
    {
        bool CanDropIn(IDraggable draggable);
        void DropIn(IDraggable view);
    }
}
