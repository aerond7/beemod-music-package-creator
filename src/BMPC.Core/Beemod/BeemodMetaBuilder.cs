using BMPC.Core.Structs;

namespace BMPC.Core.Beemod
{
    public class BeemodMetaBuilder
    {
        private string Id { get; set; }
        private string Name { get; set; }
        private string Description { get; set; }
        private List<BeemodPackMusic> MusicSections { get; set; }

        public BeemodMetaBuilder()
        {
            this.Id = string.Empty;
            this.Name = string.Empty;
            this.Description = string.Empty;
            this.MusicSections = new List<BeemodPackMusic>();
        }

        public string GetPackageId()
            => this.Id;

        public BeemodMetaBuilder WithName(string name)
        {
            this.Id = string.Format(Constants.PackageIdPattern, Utils.ConvertToSafeFileName(name).ToLowerInvariant());
            this.Name = name;
            return this;
        }

        public BeemodMetaBuilder WithDescription(string description)
        {
            this.Description = description;
            return this;
        }

        public BeemodMetaBuilder AddMusic(BeemodPackMusic music)
        {
            this.MusicSections.Add(music);
            return this;
        }

        public Stream Build()
        {
            var stream = new MemoryStream();

            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                writer.WriteLine($"// This package was generated using BMPC {Utils.GetAppVersion()}");
                writer.WriteLine("// See more information about BMPC at https://bmpc.aerond.dev/");
                writer.WriteLine();

                // write package info
                if (!string.IsNullOrWhiteSpace(this.Id))
                {
                    writer.WriteLine(new KeyValueString("ID", this.Id));
                }
                else
                {
                    throw new InvalidOperationException("ID for package cannot be missing, ID is automatically determined when setting a package name");
                }

                if (!string.IsNullOrWhiteSpace(this.Name))
                {
                    writer.WriteLine(new KeyValueString("Name", Utils.EscapeString(this.Name)));
                }

                if (!string.IsNullOrWhiteSpace(this.Description))
                {
                    writer.WriteLine(new KeyValueString("Desc", Utils.EscapeString(this.Description)));
                }

                // write all music sections
                foreach (var music in this.MusicSections)
                {
                    // initial first linebreak and start of this music section
                    writer.WriteLine();
                    writer.WriteLine(KVObjectStart("Music"));

                    // write basic music info
                    writer.WriteLine(new KeyValueString("ID", GetMusicId(music.Name)));
                    writer.WriteLine(new KeyValueString("Name", Utils.EscapeString(music.Name)));
                    if (music.ShortName is not null)
                    {
                        writer.WriteLine(new KeyValueString("ShortName", Utils.EscapeString(music.ShortName)));
                    }
                    writer.WriteLine(new KeyValueString("Group", Utils.EscapeString(music.Group)));
                    writer.WriteLine(new KeyValueString("Icon", music.Icon));
                    writer.WriteLine(new KeyValueString("IconLarge", music.IconLarge));
                    writer.WriteLine(new KeyValueString("Authors", Utils.EscapeString(music.Authors)));
                    writer.WriteLine(new KeyValueString("Description", Utils.EscapeString(music.Description)));

                    // write sample music info
                    if (music.SampleTractorBeam is null)
                    {
                        writer.WriteLine(new KeyValueString("Sample", music.SampleBase));
                    }
                    else
                    {
                        writer.WriteLine(KVObjectStart("Sample"));

                        writer.WriteLine(new KeyValueString("Base", music.SampleBase));
                        writer.WriteLine(new KeyValueString("tBeam", music.SampleTractorBeam));

                        writer.WriteLine(KVObjectEnd());
                    }

                    // write soundscript music info
                    if (music.SoundscriptTractorBeam is null &&
                        music.SpeedGelSfx.Count <= 0 &&
                        music.BounceGelSfx.Count <= 0)
                    {
                        writer.WriteLine(new KeyValueString("Soundscript", music.SoundscriptBase));
                    }
                    else
                    {
                        writer.WriteLine(KVObjectStart("Soundscript"));

                        writer.WriteLine(new KeyValueString("Base", music.SoundscriptBase));

                        if (music.SoundscriptTractorBeam is not null)
                        {
                            writer.WriteLine(new KeyValueString("tBeam", music.SoundscriptTractorBeam));
                            if (music.SyncTractorBeamMusic)
                            {
                                writer.WriteLine(new KeyValueString("sync_funnel", "1"));
                            }
                        }

                        if (music.SpeedGelSfx.Count > 0)
                        {
                            writer.WriteLine(KVObjectStart("SpeedGel"));

                            foreach (var speedGelSfx in music.SpeedGelSfx)
                            {
                                writer.WriteLine(new KeyValueString("snd", speedGelSfx));
                            }

                            writer.WriteLine(KVObjectEnd());
                        }

                        if (music.BounceGelSfx.Count > 0)
                        {
                            writer.WriteLine(KVObjectStart("BounceGel"));

                            foreach (var bounceGelSfx in music.BounceGelSfx)
                            {
                                writer.WriteLine(new KeyValueString("snd", bounceGelSfx));
                            }

                            writer.WriteLine(KVObjectEnd());
                        }

                        writer.WriteLine(KVObjectEnd());
                    }

                    // write default funnel music
                    if (music.UseDefaultTractorBeamMusic &&
                        music.SoundscriptTractorBeam is null)
                    {
                        writer.WriteLine(KVObjectStart("Children"));

                        writer.WriteLine(new KeyValueString("tBeam", "VALVE_TEST"));

                        writer.WriteLine(KVObjectEnd());
                    }
                    
                    writer.WriteLine(KVObjectEnd());
                }

                writer.Flush();
            }

            stream.Position = 0;

            return stream;
        }

        // Music IDs include the package name so they stay unique across packages.
        private string GetMusicId(string musicName)
            => string.Format(
                Constants.MusicIdPattern,
                Utils.ConvertToSafeFileName(this.Name).ToLowerInvariant(),
                Utils.ConvertToSafeFileName(musicName).ToLowerInvariant());

        private string KVObjectStart(string objectName) =>
            $"\"{objectName}\"\n{{";

        private string KVObjectEnd() =>
            $"}}";
    }

    public class BeemodPackMusic
    {
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string Group { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string IconLarge { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SampleBase { get; set; } = string.Empty;
        public string? SampleTractorBeam { get; set; }
        public string SoundscriptBase { get; set; } = string.Empty;
        public string? SoundscriptTractorBeam { get; set; }
        public bool UseDefaultTractorBeamMusic { get; set; }
        public bool SyncTractorBeamMusic { get; set; }
        public List<string> SpeedGelSfx { get; set; } = new List<string>();
        public List<string> BounceGelSfx { get; set; } = new List<string>();
    }
}
