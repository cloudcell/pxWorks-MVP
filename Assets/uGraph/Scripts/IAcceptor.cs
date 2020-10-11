// Copyright (c) 2020 Cloudcell Limited

namespace uGraph
{
    public interface IAcceptor
    {
        bool CanDropIn(IDraggable draggable);
        void DropIn(IDraggable view);
    }
}
