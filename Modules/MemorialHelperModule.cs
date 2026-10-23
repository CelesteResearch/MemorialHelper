// Copyright (c) 2020 Maildropfolder; 2022 Communal Helper Team. Licensed under the MIT License.

using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Runtime;

namespace Celeste.Mods.MemorialHelper.Modules
{
    [RuntimeTypeAlias("Celeste.Mod.MemorialHelper.MemorialModule")]
    public sealed class MemorialHelperModule : SourceEverestModule
    {
        public MemorialHelperModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public override void Load()
        {
        }

        public override void Unload()
        {
        }
    }
}
