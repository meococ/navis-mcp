using NavisMcp.Contracts;

namespace NavisMcp.Server.Playbooks;

/// <summary>
/// Built-in EN core and VN locale-overlay search packs for Phase 0 selection hygiene.
/// </summary>
public static class DomainOntology
{
    public static DomainOntologyResult ListPacks()
    {
        return new DomainOntologyResult
        {
            Packs = new List<DomainOntologyPack>
            {
                Pack(
                    "en.structure",
                    "Structure / Structural elements",
                    "structure",
                    "en",
                    new[] { "structure", "structural", "beam", "column", "slab", "foundation", "rebar", "steel" },
                    new[]
                    {
                        Term("structure", "must", "Primary structural discipline keyword"),
                        Term("structural", "must"),
                        Term("beam", "should"),
                        Term("column", "should"),
                        Term("slab", "should"),
                        Term("foundation", "list"),
                        Term("rebar", "qa", "Often nested under concrete parents")
                    },
                    new[]
                    {
                        "Generic 'structure' layer names may include non-structural envelopes.",
                        "Architectural vs structural columns often share class names — scope by source model."
                    }),
                Pack(
                    "en.mep",
                    "MEP / Mechanical-Electrical-Plumbing",
                    "mep",
                    "en",
                    new[] { "mep", "pipe", "duct", "conduit", "cable tray", "hvac", "plumbing" },
                    new[]
                    {
                        Term("mep", "must"),
                        Term("pipe", "should"),
                        Term("duct", "should"),
                        Term("conduit", "should"),
                        Term("cable tray", "list"),
                        Term("hvac", "list")
                    },
                    new[]
                    {
                        "Pipe and duct searches are noisy without sourceModelContains or scopeItemId.",
                        "Cable trays may be mislabeled as pipe in some federated exports."
                    }),
                Pack(
                    "en.architecture",
                    "Architecture / Building envelope",
                    "architecture",
                    "en",
                    new[] { "architecture", "wall", "door", "window", "curtain wall", "roof", "floor", "ceiling" },
                    new[]
                    {
                        Term("architecture", "must"),
                        Term("wall", "should"),
                        Term("door", "should"),
                        Term("window", "should"),
                        Term("curtain wall", "list"),
                        Term("roof", "list")
                    },
                    new[]
                    {
                        "Generic 'wall' matches retaining walls and highway barriers in civil models.",
                        "Interior vs exterior walls may share display names across federated sources."
                    }),
                Pack(
                    "en.civil",
                    "Civil / Infrastructure",
                    "civil",
                    "en",
                    new[] { "civil", "road", "pavement", "alignment", "corridor", "earthwork", "drainage" },
                    new[]
                    {
                        Term("civil", "must"),
                        Term("road", "should"),
                        Term("pavement", "should"),
                        Term("alignment", "should"),
                        Term("corridor", "list"),
                        Term("drainage", "list")
                    },
                    new[]
                    {
                        "Alignment curves often have no solid volume for clash.",
                        "Road solids may overlap with architectural site elements."
                    }),
                Pack(
                    "vi.cau",
                    "Cầu / Bridge structure",
                    "structure",
                    "vi",
                    new[] { "cau", "cầu", "bridge", "abutment", "pier", "deck" },
                    new[]
                    {
                        Term("cầu", "must", "Primary Vietnamese bridge keyword"),
                        Term("cau", "must", "Accent-stripped form used by find_items"),
                        Term("bridge", "should", "English synonym"),
                        Term("mố", "should", "Abutment"),
                        Term("trụ", "should", "Pier"),
                        Term("bản mặt cầu", "list", "Deck slab naming variants")
                    },
                    new[]
                    {
                        "Matches 'cầu thang' (stair) or furniture named 'cầu' are false positives — scope by source model.",
                        "Do not treat 'cầu dao' / electrical switchgear as bridge structure."
                    }),
                Pack(
                    "vi.bmc",
                    "Bê tông / Concrete",
                    "structure",
                    "vi",
                    new[] { "bmc", "bê tông", "be tong", "concrete", "rc" },
                    new[]
                    {
                        Term("bê tông", "must"),
                        Term("be tong", "must"),
                        Term("bmc", "should", "Common Vietnamese abbreviation"),
                        Term("concrete", "should"),
                        Term("cốt thép", "qa", "Rebar often nested under concrete parents")
                    },
                    new[]
                    {
                        "BMC may appear in layer codes unrelated to concrete grade.",
                        "Prefabricated vs cast-in-place naming is inconsistent across federated models."
                    }),
                Pack(
                    "vi.khe",
                    "Khe co giãn / Expansion joint",
                    "structure",
                    "vi",
                    new[] { "khe", "khe co gian", "expansion joint", "joint" },
                    new[]
                    {
                        Term("khe", "must"),
                        Term("khe co giãn", "must"),
                        Term("expansion joint", "should"),
                        Term("neoprene", "list")
                    },
                    new[]
                    {
                        "'Khe' alone matches many Vietnamese words; prefer 'khe co' / 'expansion'.",
                        "Joint sealant vs structural gap may share display names."
                    }),
                Pack(
                    "vi.lan_can",
                    "Lan can / Guardrail",
                    "structure",
                    "vi",
                    new[] { "lan can", "lan_can", "guardrail", "railing", "parapet" },
                    new[]
                    {
                        Term("lan can", "must"),
                        Term("guardrail", "should"),
                        Term("railing", "should"),
                        Term("parapet", "list")
                    },
                    new[]
                    {
                        "Architectural balcony railings may collide with highway guardrail searches.",
                        "Steel vs concrete parapet often live in different source models."
                    }),
                Pack(
                    "vi.duong",
                    "Đường / Roadway",
                    "civil",
                    "vi",
                    new[] { "duong", "đường", "road", "pavement", "carriageway" },
                    new[]
                    {
                        Term("đường", "must"),
                        Term("duong", "must"),
                        Term("road", "should"),
                        Term("lề đường", "list"),
                        Term("vỉa hè", "exclude", "Sidewalk — often out of clash scope")
                    },
                    new[]
                    {
                        "'Đường ống' (pipe) is a common false positive for roadway searches.",
                        "Alignment vs pavement solids may both contain 'đường'."
                    }),
                Pack(
                    "vi.ong",
                    "Ống / MEP pipe",
                    "mep",
                    "vi",
                    new[] { "ong", "ống", "pipe", "duct", "ống nước", "ống gió" },
                    new[]
                    {
                        Term("ống", "must"),
                        Term("ong", "must"),
                        Term("pipe", "should"),
                        Term("duct", "should"),
                        Term("ống nước", "list"),
                        Term("ống gió", "list")
                    },
                    new[]
                    {
                        "Accent-stripped 'ong' is very noisy — always pair with scopeItemId or sourceModelContains.",
                        "Cable trays may be mislabeled as ống in some exports."
                    }),
                Pack(
                    "vi.cot",
                    "Cột / Column / Pole",
                    "structure",
                    "vi",
                    new[] { "cot", "cột", "column", "pier", "pole" },
                    new[]
                    {
                        Term("cột", "must"),
                        Term("cot", "must"),
                        Term("column", "should"),
                        Term("cột điện", "exclude", "Utility pole — discipline dependent")
                    },
                    new[]
                    {
                        "'Cột' matches lighting poles, structural columns, and sign posts.",
                        "Use class/category filters when available."
                    }),
                Pack(
                    "linear.corridor",
                    "Linear corridor / ROW",
                    "civil",
                    "en",
                    new[] { "corridor", "right of way", "row", "alignment", "tuyến" },
                    new[]
                    {
                        Term("corridor", "must"),
                        Term("alignment", "should"),
                        Term("ROW", "list"),
                        Term("tuyến", "should", "Vietnamese synonym for linear alignment")
                    },
                    new[]
                    {
                        "Corridor solids may be oversized envelopes — not clash geometry.",
                        "Alignment curves often have no solid volume for clash."
                    })
            }
        };
    }

    private static DomainOntologyPack Pack(
        string id,
        string displayName,
        string discipline,
        string locale,
        IEnumerable<string> queries,
        IEnumerable<DomainOntologyTerm> terms,
        IEnumerable<string> falsePositives)
    {
        return new DomainOntologyPack
        {
            Id = id,
            DisplayName = displayName,
            Discipline = discipline,
            Locale = locale,
            SearchQueries = queries.ToList(),
            Terms = terms.ToList(),
            FalsePositiveNotes = falsePositives.ToList()
        };
    }

    private static DomainOntologyTerm Term(string term, string kind, string? note = null)
    {
        return new DomainOntologyTerm { Term = term, Kind = kind, Note = note };
    }
}
