using NAudio.Midi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MidiTweeter
{
    public static class Program
    {
        private static readonly string[] FormatTypeText = 
        {
            "0 (One track containing all of the MIDI events)",
            "1 (Two or more tracks. First track containing metadata)", 
            "2 (Multiple tracks, different sequences)"
        };

        public static void Main(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args), "argument array cannot be null");
            }

            if (args.Length > 0)
            {
                if (args[0].ToLower(CultureInfo.InvariantCulture).Contains("lorie"))    // easter egg
                {
                    Console.BackgroundColor = ConsoleColor.Magenta;
                    Console.ForegroundColor = ConsoleColor.Green;
                }
            }

            Console.WriteLine("MIDI Tweeter Player v1.2 (c) 2010 Alex");
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("");

            if (args.Length == 0)
            {
                DisplayUsage();
                return;
            }

            var tracksToSkip = new List<int>();
            var halfStepOffset = 0;
            double tempoRatio = 1;
            Comparison<Note> noteComparisonMethod = CompareNoteByHighNumberThenHighVelocity;
            var quietMode = false;
            var noPercussion = false;

            var filename = args[0];

            var first = true;

            foreach (var arg in args)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                try
                {
                    if (arg.StartsWith("-S"))
                    {
                        tracksToSkip.Add(int.Parse(arg.Substring(2)));
                    }
                    else if (arg == "-NOPERC")
                    {
                        noPercussion = true;
                    }
                    else if (arg.StartsWith("-P"))
                    {
                        halfStepOffset = int.Parse(arg.Substring(2));
                    }
                    else if (arg.StartsWith("-T"))
                    {
                        tempoRatio = double.Parse(arg.Substring(2));
                    }
                    else if (arg.StartsWith("-C"))
                    {
                        var parameter = int.Parse(arg.Substring(2));

                        switch (parameter)
                        {
                            case 1:
                                noteComparisonMethod = CompareNoteByHighVelocityThenHighNumber;
                                break;
                            case 2:
                                noteComparisonMethod = CompareNoteByHighVelocityThenLowNumber;
                                break;
                            case 3:
                                noteComparisonMethod = CompareNoteByHighNumberThenHighVelocity;
                                break;
                            case 4:
                                noteComparisonMethod = CompareNoteByLowNumberThenHighVelocity;
                                break;
                            default:
                                throw new ArgumentException("Invalid parameter.");
                        }
                    }
                    else if (arg.StartsWith("-Q"))
                    {
                        quietMode = true;
                    }
                    else
                    {
                        Console.WriteLine("Error: Invalid option: {0}", arg);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Warning: {0} (Using default value)", e.Message);
                }
            }

            try
            {
                Console.WriteLine("Parsing MIDI file...");

                var mf = new MidiFile(filename);

                Console.WriteLine("Midi Format Type: {0}", FormatTypeText[mf.FileFormat]);
                Console.WriteLine("Ticks per beat: {0}", mf.DeltaTicksPerQuarterNote);

                if ((mf.DeltaTicksPerQuarterNote & 0x8000) != 0)
                {
                    Console.WriteLine("Error: This value is actually frames per second : {0}.", mf.DeltaTicksPerQuarterNote);
                    return;
                }

                Console.WriteLine("Tracks: {0}", mf.Tracks);
                Console.WriteLine("");

                var timeSliceList = ReadMidiFile(mf, tracksToSkip, tempoRatio, noPercussion);

                timeSliceList = RemoveSliceOverlapping(timeSliceList);

                SortNoteLists(timeSliceList, noteComparisonMethod);

                Console.WriteLine("");

                DisplayStats(timeSliceList);

                Console.WriteLine("");

                if (quietMode == false)
                {
                    Console.WriteLine("Playing...");
                    Play(timeSliceList, halfStepOffset);
                    Console.WriteLine("Done!");
                }

                Console.WriteLine("Generating wave file...");
                GenerateWaveFile(timeSliceList, halfStepOffset);
                Console.WriteLine("Done!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }

        private static void SortNoteLists(IEnumerable<TimeSlice> timeSliceList, Comparison<Note> comparison)
        {
            foreach (var ts in timeSliceList)
            {
                ts.NoteList.Sort(comparison);
            }
        }

        private static int CompareNoteByHighVelocityThenHighNumber(Note x, Note y)
        {
            var velocityComparison = y.Velocity - x.Velocity;

            if (velocityComparison == 0)
                return y.NoteNumber - x.NoteNumber;

            return velocityComparison;
        }

        private static int CompareNoteByHighVelocityThenLowNumber(Note x, Note y)
        {
            var velocityComparison = y.Velocity - x.Velocity;

            if (velocityComparison == 0)
                return x.NoteNumber - y.NoteNumber;

            return velocityComparison;
        }

        private static int CompareNoteByHighNumberThenHighVelocity(Note x, Note y)
        {
            var numberComparison = y.NoteNumber - x.NoteNumber;

            if (numberComparison == 0)
                return y.Velocity - x.Velocity;

            return numberComparison;
        }

        private static int CompareNoteByLowNumberThenHighVelocity(Note x, Note y)
        {
            var numberComparison = x.NoteNumber - y.NoteNumber;

            if (numberComparison == 0)
                return y.Velocity - x.Velocity;

            return numberComparison;
        }

        private static void DisplayUsage()
        {
            Console.WriteLine("Usage: MidiTweeter <filename> [options ...]");
            Console.WriteLine("Options:");
            Console.WriteLine("\t-S<trackNumber>\t\tSuprress a track");
            Console.WriteLine("\t-NOPERC\t\tSuprress percussions");
            Console.WriteLine("\t-P<noteOffset>\t\tModify pitch (in half steps)");
            Console.WriteLine("\t-T<tempoRatio>\t\tSpecify a tempo ratio");
            Console.WriteLine("\t-Q\t\t\tQuiet mode");
            Console.WriteLine("\t-C<chordMethod>\t\tSpecify chord-to-note conversion method");
            Console.WriteLine("\t\t1 = High Velocity Then High Number");
            Console.WriteLine("\t\t2 = High Velocity then Low Number");
            Console.WriteLine("\t\t3 = High Number then High Velocity");
            Console.WriteLine("\t\t4 = Low Number then High Velocity");
        }

        private static List<TimeSlice> ReadMidiFile(MidiFile mf, ICollection<int> tracksToSkip, double tempoRatio, bool skipDrums)
        {
            var timeSliceList = new List<TimeSlice>();

            var tempo = (int)Math.Round(120 * tempoRatio);

            Console.WriteLine("Default Tempo: {0}", tempo);

            var trackNumber = 0;

            foreach (var midiEventList in mf.Events)
            {
                Console.WriteLine("");
                Console.WriteLine("Track #{0}", trackNumber);
                Console.WriteLine("---------");

                if (tracksToSkip.Contains(trackNumber))
                {
                    trackNumber++;
                    continue;
                }

                Console.WriteLine("Track length: {0}", midiEventList.Count);

                var drumFilteredMidiEventList =  midiEventList.Where(midiEvent => !skipDrums || (midiEvent.Channel != 10));

                foreach (var midiEvent in drumFilteredMidiEventList)
                {
                    if (midiEvent is TextEvent textEvent) // : MetaEvent
                    {
                        Console.WriteLine("{0}: {1}", textEvent.MetaEventType.ToString(), textEvent.Text);
                    }
                    else if (midiEvent is TimeSignatureEvent timeSignatureEvent) // : MetaEvent
                    {
                        //WriteWarningLine("TimeSignatureEvent", timeSignatureEvent.ToString());
                    }
                    else if (midiEvent is TrackSequenceNumberEvent trackSequenceNumberEvent) // : MetaEvent
                    {
                        WriteWarningLine("TrackSequenceNumberEvent", trackSequenceNumberEvent.ToString());
                    }
                    else if (midiEvent is KeySignatureEvent keySignatureEvent) // : MetaEvent
                    {
                        //WriteWarningLine("KeySignatureEvent", keySignatureEvent.ToString());
                    }
                    else if (midiEvent is TempoEvent tempoEvent) // : MetaEvent
                    {
                        tempo = (int)Math.Round(tempoEvent.Tempo * tempoRatio);
                        Console.WriteLine("Tempo: {0}", tempo);
                    }
                    else if (midiEvent is MetaEvent metaEvent)
                    {
                        if ((metaEvent.MetaEventType != MetaEventType.SequencerSpecific) && (metaEvent.MetaEventType != MetaEventType.EndTrack) && (metaEvent.MetaEventType != MetaEventType.MidiChannel) && (metaEvent.MetaEventType != MetaEventType.MidiPort))
                            WriteWarningLine("MetaEvent", metaEvent.ToString());
                    }
                    else if (midiEvent is ControlChangeEvent controlChangeEvent)
                    {
                        //WriteWarningLine("ControlChangeEvent", controlChangeEvent.ToString());
                    }
                    else if (midiEvent is PatchChangeEvent patchChangeEvent)
                    {
                        Console.WriteLine("Patch: {0}", PatchChangeEvent.GetPatchName(patchChangeEvent.Patch));

                        if (skipDrums && (patchChangeEvent.Patch >= 112))
                        {
                            Console.WriteLine("Skipping track...");
                            break;
                        }
                    }
                    else if (midiEvent is NoteOnEvent noteEvent1)
                    {
                        if (MidiEvent.IsNoteOff(noteEvent1) == false)
                        {
                            if (noteEvent1.OffEvent != null)
                            {
                                var ticksPerMinute = mf.DeltaTicksPerQuarterNote * tempo;
                                var millisecondsPerTick = 60.0 * 1000.0 / ticksPerMinute;

                                var start = (int)Math.Round(noteEvent1.AbsoluteTime * millisecondsPerTick);
                                var stop = (int)Math.Round(noteEvent1.OffEvent.AbsoluteTime * millisecondsPerTick);

                                timeSliceList.Add(new TimeSlice(start, stop, new Note(noteEvent1.NoteNumber, noteEvent1.Velocity)));
                            }
                            else
                                WriteWarningLine("NoteOnEvent", "No Corresponding Note Off Event");
                        }
                    }
                    else if (midiEvent is NoteEvent noteEvent)
                    {
                        if (noteEvent.CommandCode != MidiCommandCode.NoteOff)
                            WriteWarningLine("NoteEvent", noteEvent.ToString());
                    }
                    else
                    {
                        WriteWarningLine("MIDI Event Type", midiEvent.GetType().ToString());
                    }
                }

                trackNumber++;
            }

            timeSliceList.Sort(CompareTimeSlicesByStartTime);

            return timeSliceList;
        }

        private static void WriteWarningLine(string name, string value)
        {
            var previousColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Warning: Skipped {0}: {1}", name, value);
            Console.ForegroundColor = previousColor;
        }

        private static int CompareTimeSlicesByStartTime(TimeSlice x, TimeSlice y)
        {
            return x.Start - y.Start;
        }

        private static List<TimeSlice> RemoveSliceOverlapping(List<TimeSlice> timeSliceList)
        {
            var timePoints = new List<int>();

            foreach (var slice in timeSliceList)
            {
                if (timePoints.Contains(slice.Start) == false)
                    timePoints.Add(slice.Start);

                if (timePoints.Contains(slice.Stop) == false)
                    timePoints.Add(slice.Stop);
            }

            timePoints.Sort();

            var newList = new List<TimeSlice>();

            for (var i = 0; i < timePoints.Count - 1; i++)
            {
                newList.Add(new TimeSlice(timePoints[i], timePoints[i + 1]));
            }

            foreach (var slice in timeSliceList)
            {
                foreach (var newSlice in newList)
                {
                    if ((newSlice.Start >= slice.Start) && (newSlice.Stop <= slice.Stop))
                    {
                        newSlice.AddNoteList(slice.NoteList);
                    }
                }
            }

            return newList;
        }

        private static void DisplayStats(IEnumerable<TimeSlice> timeSliceList)
        {
            var chordCount = 0;
            var singleNoteCount = 0;
            var silenceCount = 0;

            foreach (var ts in timeSliceList)
            {
                if (ts.NoteList.Count == 0)
                    silenceCount++;
                else if (ts.NoteList.Count == 1)
                    singleNoteCount++;
                else if (ts.NoteList.Count > 1)
                    chordCount++;
            }

            Console.WriteLine("Chords: {0}", chordCount);
            Console.WriteLine("Single notes: {0}", singleNoteCount);
            Console.WriteLine("Silences: {0}", silenceCount);
        }

        private static void Play(IEnumerable<TimeSlice> timeSliceList, int halfStepOffset)
        {
            foreach (var ts in timeSliceList)
            {
                var duration = ts.Stop - ts.Start;

                if (ts.NoteList.Count == 0)
                {
                    NativeMethods.Sleep((uint)duration);
                }
                else
                {
                    var noteNumber = ts.NoteList[0].NoteNumber;

                    NativeMethods.Beep(GetMidiNoteFrequency(noteNumber + halfStepOffset), (uint)duration);
                }
            }
        }

        private static void GenerateWaveFile(IEnumerable<TimeSlice> timeSliceList, int halfStepOffset)
        {
            var waveFormat = new WaveFormat(44100, 16, 1);

            using (var writer = new WaveFileWriter("fileName.wav", waveFormat))
            {
                foreach (var ts in timeSliceList)
                {
                    var duration = ts.Stop - ts.Start;

                    var sampleCount = (int)Math.Round(44.1*duration);
                    var sampleBuffer = new int[sampleCount];

                    if (ts.NoteList.Count == 0)
                    {
                        // Buffer is already set to 0.
                    }
                    else
                    {
                        foreach (var note in ts.NoteList)
                        {
                            var noteNumber = note.NoteNumber;
                            var shiftedNote = noteNumber + halfStepOffset;
                            var frequency = GetMidiNoteFrequency(shiftedNote);
                            var volume = note.Velocity/127.0;

                            var patternBuffer = GetPatternBuffer(frequency, volume);

                            var j = 0;

                            for (var i = 0; i < sampleBuffer.Length; i++)
                            {
                                sampleBuffer[i] += patternBuffer[j];
                                
                                if (sampleBuffer[i] > 65535)
                                {
                                    sampleBuffer[i] = 65535;
                                }

                                j++;

                                if (j == patternBuffer.Length)
                                {
                                    j = 0;
                                }
                            }
                        }
                    }

                    var sampleByteArray = ToByteArray(sampleBuffer);
                    writer.Write(sampleByteArray, 0, sampleByteArray.Length);
                }
            }
        }

        private static byte[] ToByteArray(int[] intArray)
        {
            var buffer = new byte[intArray.Length * 2];
            var x = 0;

            foreach (var value in intArray)
            {
                buffer[x++] = (byte) (value & 0xff);
                buffer[x++] = (byte) ((value >> 8) & 0xff);
            }

            return buffer;
        }

        private static int[] GetPatternBuffer(uint frequency, double volume)
        {
            var maxSampleValue = 0x1FFF * volume;
            var decimalSampleCount = 44100.0 / frequency;
            var sampleCount = (int)Math.Round(decimalSampleCount);
            var sampleBuffer = new int[sampleCount];

            var sampleEnvelope = new double[sampleCount];

            for (var i = 0; i < sampleBuffer.Length; i++)
            {
                if (i < sampleBuffer.Length - 10)
                {
                    sampleEnvelope[i] = 1.0;
                }
                else
                {
                    sampleEnvelope[i] = (sampleBuffer.Length - i - 1) / 10.0;
                }
            }

            for (var i = 0; i < sampleBuffer.Length; i++)
            {
                // sinusoidal signal
                var sineValue = (i * 2.0 * Math.PI) / (sampleBuffer.Length - 1);
                var sampleValue = (int)(Math.Round((Math.Sin(sineValue) + 1.0) * maxSampleValue / 2.0) * sampleEnvelope[i]);
                sampleBuffer[i] = sampleValue;
            }

            return sampleBuffer;
        }

        private static uint GetMidiNoteFrequency(int noteNumber)
        {
            return (uint)((440.0 / 32.0) * Math.Pow(2, (noteNumber - 9) / 12.0));
        }
    }
}
