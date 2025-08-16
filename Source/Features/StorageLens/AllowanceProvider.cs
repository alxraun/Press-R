namespace PressR.Features.StorageLens
{
    public class AllowanceProvider(State state)
    {
        private readonly State _state = state;

        public void Update()
        {
            _state.AllowanceStatesForSelectedStorage.Clear();

            var storage = _state.SelectedStorage;
            if (storage == null)
            {
                return;
            }

            var filter = storage.GetStoreSettings()?.filter;
            if (filter == null)
            {
                return;
            }

            foreach (var thing in _state.StorableForSelectedStorageInView)
            {
                if (thing?.def != null)
                {
                    if (!_state.AllowanceStatesForSelectedStorage.ContainsKey(thing.def))
                    {
                        _state.AllowanceStatesForSelectedStorage[thing.def] = filter.Allows(
                            thing.def
                        );
                    }
                }
            }
        }
    }
}
