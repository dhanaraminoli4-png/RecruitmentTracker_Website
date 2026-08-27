from datetime import datetime
import re
from collections import Counter

from sentence_transformers import SentenceTransformer
from sklearn.metrics.pairwise import cosine_similarity


# ============================================================
# LOAD AI MODEL
# ============================================================

print("Loading AI model...")

model = SentenceTransformer("all-MiniLM-L6-v2")

print("AI model loaded successfully.\n")


# ============================================================
# TEXT NORMALIZATION
# ============================================================

def normalize_text(text):

    if text is None:
        return ""

    text = str(text).lower()

    text = re.sub(
        r"[^a-z0-9+#.\s/-]",
        " ",
        text
    )

    text = re.sub(
        r"\s+",
        " ",
        text
    )

    return text.strip()


# ============================================================
# SAFE FLOAT
# ============================================================

def safe_float(value):

    try:
        return float(value)
    except Exception:
        return 0.0


# ============================================================
# EXPERIENCE SECTION
# ============================================================

def extract_experience_section(cv_text):

    if not cv_text:
        return ""

    lines = cv_text.splitlines()

    start_index = None
    end_index = len(lines)

    experience_headers = {
        "experience",
        "work experience",
        "professional experience",
        "employment history",
        "work history",
        "career history"
    }

    ending_headers = {
        "education",
        "skills",
        "technical skills",
        "certifications",
        "projects",
        "languages",
        "strengths",
        "key achievements",
        "achievements",
        "references",
        "interests",
        "awards"
    }

    for i, line in enumerate(lines):

        cleaned = normalize_text(line).strip(" :.-")

        if cleaned in experience_headers:
            start_index = i + 1
            break

    if start_index is None:
        return cv_text

    for i in range(start_index, len(lines)):

        cleaned = normalize_text(lines[i]).strip(" :.-")

        if cleaned in ending_headers:

            end_index = i
            break

    return "\n".join(
        lines[start_index:end_index]
    )


# ============================================================
# PARSE DATE
# ============================================================

def parse_date_token(token):

    token = token.strip().lower()

    if token in ["present", "current", "now"]:

        return (
            datetime.now().year,
            datetime.now().month
        )

    match = re.fullmatch(
        r"(\d{1,2})[/-](\d{4})",
        token
    )

    if match:

        month = int(match.group(1))
        year = int(match.group(2))

        month = max(
            1,
            min(month, 12)
        )

        return year, month

    match = re.fullmatch(
        r"\d{4}",
        token
    )

    if match:
        return int(token), 1

    return None


# ============================================================
# EXTRACT EXPERIENCE PERIODS
# ============================================================

def extract_experience_periods(cv_text):

    experience_text = extract_experience_section(cv_text)

    if not experience_text:
        return []

    date_pattern = re.compile(

    r"(?P<start>"
    r"(?:(?:0?[1-9]|1[0-2])[/\-])?"
    r"\d{4}"
    r")"

    r"\s*"

    r"(?:-|to)"

    r"\s*"

    r"(?P<end>"
    r"(?:(?:(?:0?[1-9]|1[0-2])[/\-])?"
    r"\d{4})"
    r"|present"
    r"|current"
    r"|now"
    r")",

    re.IGNORECASE
)

    periods = []

    for match in date_pattern.finditer(
        experience_text
    ):

        start = parse_date_token(
            match.group("start")
        )

        end = parse_date_token(
            match.group("end")
        )

        if start is None or end is None:
            continue

        start_year, start_month = start
        end_year, end_month = end

        start_total = (
            start_year * 12
            + start_month
        )

        end_total = (
            end_year * 12
            + end_month
        )

        if end_total >= start_total:

            periods.append(
                (
                    start_total,
                    end_total
                )
            )

    return periods


# ============================================================
# EXTRACT YEARS OF EXPERIENCE
# ============================================================

def extract_years_of_experience(cv_text):

    if not cv_text:
        return 0.0

    experience_text = extract_experience_section(
        cv_text
    )

    numbers = []

    # --------------------------------------------------------
    # EXPLICIT YEARS
    # --------------------------------------------------------

    numeric_matches = re.findall(

        r"(\d+(?:\.\d+)?)\s*\+?\s*years?",

        experience_text.lower()

    )

    for value in numeric_matches:

        try:

            numbers.append(
                float(value)
            )

        except ValueError:
            pass

    # --------------------------------------------------------
    # WRITTEN NUMBERS
    # --------------------------------------------------------

    word_numbers = {

        "one": 1,
        "two": 2,
        "three": 3,
        "four": 4,
        "five": 5,
        "six": 6,
        "seven": 7,
        "eight": 8,
        "nine": 9,
        "ten": 10

    }

    for word, value in word_numbers.items():

        pattern = (
            r"\b"
            + word
            + r"\s*\+?\s*years?\b"
        )

        if re.search(
            pattern,
            experience_text.lower()
        ):

            numbers.append(
                float(value)
            )

    # --------------------------------------------------------
    # DATE RANGES
    # --------------------------------------------------------

    periods = extract_experience_periods(
        cv_text
    )

    periods.sort()

    merged = []

    for start, end in periods:

        if not merged:

            merged.append(
                [start, end]
            )

        elif start <= merged[-1][1] + 1:

            merged[-1][1] = max(
                merged[-1][1],
                end
            )

        else:

            merged.append(
                [start, end]
            )

    total_months = sum(

        end - start + 1

        for start, end in merged

    )

    date_based_years = (
        total_months / 12
    )

    if numbers:

        return round(
            max(
                max(numbers),
                date_based_years
            ),
            2
        )

    return round(
        date_based_years,
        2
    )


# ============================================================
# REQUIRED EXPERIENCE
# ============================================================

def extract_required_experience(requirement):

    text = normalize_text(
        requirement
    )

    patterns = [

        r"at least\s+(\d+(?:\.\d+)?)\s*\+?\s*years?",

        r"minimum\s+(?:of\s+)?(\d+(?:\.\d+)?)\s*\+?\s*years?",

        r"(\d+(?:\.\d+)?)\s*\+?\s*years?"

    ]

    for pattern in patterns:

        match = re.search(
            pattern,
            text
        )

        if match:

            return float(
                match.group(1)
            )

    word_numbers = {

        "one": 1,
        "two": 2,
        "three": 3,
        "four": 4,
        "five": 5,
        "six": 6,
        "seven": 7,
        "eight": 8,
        "nine": 9,
        "ten": 10

    }

    for word, value in word_numbers.items():

        pattern = (
            r"\b"
            + word
            + r"\s*\+?\s*years?\b"
        )

        if re.search(
            pattern,
            text
        ):

            return float(value)

    return None


# ============================================================
# EXPERIENCE REQUIREMENT EVALUATION
# ============================================================

def evaluate_experience(
    requirement,
    cv_text
):

    required_years = (
        extract_required_experience(
            requirement
        )
    )

    if required_years is None:
        return None

    candidate_years = (
        extract_years_of_experience(
            cv_text
        )
    )

    if candidate_years >= required_years:

        return {

            "requirement": requirement,
            "score": 100.0,
            "status": "Matched",
            "method": "Experience comparison",
            "required_years": safe_float(
                required_years
            ),
            "candidate_years": safe_float(
                candidate_years
            )

        }

    if candidate_years > 0:

        percentage = (
            candidate_years
            /
            required_years
        ) * 100

        return {

            "requirement": requirement,

            "score": safe_float(
                round(
                    min(
                        percentage,
                        99
                    ),
                    2
                )
            ),

            "status": "Partial",

            "method":
                "Experience comparison",

            "required_years":
                safe_float(required_years),

            "candidate_years":
                safe_float(candidate_years)

        }

    return {

        "requirement": requirement,
        "score": 0.0,
        "status": "Not Matched",
        "method": "Experience comparison",
        "required_years":
            safe_float(required_years),
        "candidate_years": 0.0

    }


# ============================================================
# SKILL ALIASES
# ============================================================

SKILL_ALIASES = {

    "python": [
        "python",
        "python programming",
        "python development"
    ],

    "sql": [
        "sql",
        "structured query language"
    ],

    "pandas": ["pandas"],

    "numpy": ["numpy"],

    "scikit-learn": [
        "scikit-learn",
        "scikit learn",
        "sklearn"
    ],

    "tensorflow": ["tensorflow"],

    "pytorch": [
        "pytorch",
        "torch"
    ],

    "keras": ["keras"],

    "machine learning": [
        "machine learning",
        "machine-learning",
        "ml"
    ],

    "deep learning": [
        "deep learning",
        "deep-learning",
        "neural networks",
        "neural network"
    ],

    "natural language processing": [
        "natural language processing",
        "nlp"
    ],

    "computer vision": [
        "computer vision",
        "opencv"
    ],

    "data analysis": [
        "data analysis",
        "data analytics",
        "analytical skills",
        "analyzing data"
    ],

    "data science": [
        "data science",
        "data scientist"
    ],

    "statistics": [
        "statistics",
        "statistical analysis",
        "statistical methods"
    ],

    "data visualization": [
        "data visualization",
        "data visualisation",
        "visualization",
        "visualisation",
        "data dashboards"
    ],

    "power bi": [
        "power bi",
        "powerbi"
    ],

    "tableau": ["tableau"],

    "docker": [
        "docker",
        "containerization",
        "containerisation"
    ],

    "kubernetes": [
        "kubernetes",
        "k8s"
    ],

    "aws": [
        "aws",
        "amazon web services"
    ],

    "azure": [
        "azure",
        "microsoft azure"
    ],

    "google cloud": [
        "google cloud",
        "gcp"
    ],

    "git": [
        "git",
        "git version control"
    ],

    "github": ["github"],

    "transformers": [
        "transformers",
        "transformer models"
    ],

    "hugging face": [
        "hugging face",
        "huggingface"
    ],

    "generative ai": [
        "generative ai",
        "gen ai",
        "generative artificial intelligence"
    ],

    "large language models": [
        "large language models",
        "large language model",
        "llm",
        "llms"
    ],

    "model deployment": [
        "model deployment",
        "deploying models",
        "model serving",
        "ml deployment"
    ],

    "flask": ["flask"],

    "fastapi": ["fastapi"],

    "django": ["django"],

    "java": ["java"],

    "c++": ["c++"],

    "c#": [
        "c#",
        "c sharp"
    ],

    "javascript": [
        "javascript",
        "java script"
    ],

    "typescript": ["typescript"],

    "html": ["html"],

    "css": ["css"],

    "react": [
        "react",
        "reactjs",
        "react.js"
    ],

    "angular": [
        "angular",
        "angularjs"
    ],

    "node.js": [
        "node.js",
        "nodejs",
        "node js"
    ]

}


# ============================================================
# SKILL DETECTION
# ============================================================

def skill_present(
    skill,
    text
):

    skill = normalize_text(skill)
    text = normalize_text(text)

    if not skill:
        return False

    pattern = (
        r"(?<!\w)"
        + re.escape(skill)
        + r"(?!\w)"
    )

    return re.search(
        pattern,
        text
    ) is not None


def skills_found(text):

    found = set()

    for canonical, aliases in SKILL_ALIASES.items():

        for alias in aliases:

            if skill_present(
                alias,
                text
            ):

                found.add(
                    canonical
                )

                break

    return found


# ============================================================
# DIRECT / ALIAS SCORE
# ============================================================

def direct_or_alias_score(
    requirement,
    cv_text
):

    requirement_clean = normalize_text(
        requirement
    )

    cv_clean = normalize_text(
        cv_text
    )

    if not requirement_clean:
        return None

    # Exact phrase
    if requirement_clean in cv_clean:
        return 100.0

    requirement_skills = skills_found(
        requirement_clean
    )

    if requirement_skills:

        candidate_skills = skills_found(
            cv_clean
        )

        matched = (
            requirement_skills
            &
            candidate_skills
        )

        ratio = (
            len(matched)
            /
            len(requirement_skills)
        )

        if ratio >= 1:
            return 100.0

        if ratio > 0:

            return round(
                50 + (ratio * 50),
                2
            )

    stop_words = {

        "and",
        "or",
        "the",
        "a",
        "an",
        "with",
        "in",
        "of",
        "to",
        "for",
        "on",
        "experience",
        "knowledge",
        "skills",
        "skill",
        "strong",
        "good",
        "understanding",
        "proficiency",
        "proficient",
        "ability",
        "working",
        "work"

    }

    requirement_words = {

        word

        for word
        in requirement_clean.split()

        if word not in stop_words
        and len(word) > 1

    }

    cv_words = set(
        cv_clean.split()
    )

    if requirement_words:

        matched_words = (
            requirement_words
            &
            cv_words
        )

        word_ratio = (

            len(matched_words)
            /
            len(requirement_words)

        )

        if word_ratio >= 0.80:
            return 90.0

        if word_ratio >= 0.60:
            return 75.0

    return None


# ============================================================
# DEGREE DETECTION
# ============================================================

def is_degree_requirement(
    requirement
):

    text = normalize_text(
        requirement
    )

    degree_words = [

        "bachelor",
        "bachelors",
        "bsc",
        "b.sc",
        "master",
        "masters",
        "msc",
        "m.sc",
        "phd",
        "doctorate",
        "degree"

    ]

    return any(
        word in text
        for word in degree_words
    )


# ============================================================
# DEGREE EVALUATION
# ============================================================

def evaluate_degree_requirement(
    requirement,
    cv_text
):

    requirement_clean = normalize_text(
        requirement
    )

    cv_clean = normalize_text(
        cv_text
    )

    degree_type = None

    if (
        "bachelor" in requirement_clean
        or "bsc" in requirement_clean
        or "b.sc" in requirement_clean
    ):

        degree_type = "bachelor"

    elif (
        "master" in requirement_clean
        or "msc" in requirement_clean
        or "m.sc" in requirement_clean
    ):

        degree_type = "master"

    elif (
        "phd" in requirement_clean
        or "doctorate" in requirement_clean
    ):

        degree_type = "phd"

    # --------------------------------------------------------
    # DEGREE TYPE
    # --------------------------------------------------------

    degree_matched = False

    if degree_type == "bachelor":

        degree_matched = any(
            item in cv_clean
            for item in [
                "bachelor",
                "bachelors",
                "bsc",
                "b.sc"
            ]
        )

    elif degree_type == "master":

        degree_matched = any(
            item in cv_clean
            for item in [
                "master",
                "masters",
                "msc",
                "m.sc"
            ]
        )

    elif degree_type == "phd":

        degree_matched = any(
            item in cv_clean
            for item in [
                "phd",
                "doctorate"
            ]
        )

    else:

        degree_matched = (
            "degree"
            in cv_clean
        )

    if not degree_matched:

        return {

            "requirement":
                requirement,

            "score":
                0.0,

            "status":
                "Not Matched",

            "method":
                "Degree matching"

        }

    # --------------------------------------------------------
    # FIELD
    # --------------------------------------------------------

    fields = [

        "computer science",
        "artificial intelligence",
        "machine learning",
        "data science",
        "software engineering",
        "information technology",
        "information systems",
        "computer engineering",
        "mathematics",
        "statistics",
        "engineering"

    ]

    required_fields = [

        field

        for field in fields

        if field in requirement_clean

    ]

    if not required_fields:

        return {

            "requirement":
                requirement,

            "score":
                100.0,

            "status":
                "Matched",

            "method":
                "Degree matching"

        }

    for field in required_fields:

        if field in cv_clean:

            return {

                "requirement":
                    requirement,

                "score":
                    100.0,

                "status":
                    "Matched",

                "method":
                    "Degree and field matching"

            }

    if "related field" in requirement_clean:

        return {

            "requirement":
                requirement,

            "score":
                80.0,

            "status":
                "Partial",

            "method":
                "Degree matching"

        }

    return {

        "requirement":
            requirement,

        "score":
            70.0,

        "status":
            "Partial",

        "method":
            "Degree type matching"

    }


# ============================================================
# TEXT CHUNKS
# ============================================================

def create_text_chunks(
    text,
    max_chunks=80
):

    if not text:
        return []

    raw_parts = re.split(
        r"[\n.!?]+",
        text
    )

    chunks = []

    for part in raw_parts:

        cleaned = part.strip()

        if not cleaned:
            continue

        words = cleaned.split()

        if len(words) <= 70:

            chunks.append(
                cleaned
            )

        else:

            for i in range(
                0,
                len(words),
                60
            ):

                chunks.append(
                    " ".join(
                        words[i:i + 60]
                    )
                )

    return chunks[:max_chunks]


# ============================================================
# SEMANTIC SIMILARITY
# ============================================================

def semantic_similarity(
    requirement,
    cv_text
):

    if (
        not requirement
        or not cv_text
    ):
        return 0.0

    embeddings = model.encode(

        [
            str(requirement),
            str(cv_text)
        ],

        normalize_embeddings=True

    )

    similarity = cosine_similarity(

        [embeddings[0]],

        [embeddings[1]]

    )[0][0]

    return round(

        max(
            0.0,
            min(
                safe_float(
                    similarity * 100
                ),
                100.0
            )
        ),

        2

    )


# ============================================================
# BEST SEMANTIC MATCH
# ============================================================

def best_semantic_similarity(
    requirement,
    cv_text
):

    if (
        not requirement
        or not cv_text
    ):
        return 0.0

    chunks = create_text_chunks(
        cv_text
    )

    if not chunks:

        return semantic_similarity(
            requirement,
            cv_text
        )

    requirement_embedding = model.encode(

        [str(requirement)],

        normalize_embeddings=True

    )[0]

    chunk_embeddings = model.encode(

        chunks,

        normalize_embeddings=True

    )

    scores = cosine_similarity(

        [requirement_embedding],

        chunk_embeddings

    )[0]

    best_score = max(

        safe_float(score)

        for score in scores

    )

    return round(

        max(
            0.0,
            min(
                best_score * 100,
                100.0
            )
        ),

        2

    )


# ============================================================
# REQUIREMENT EVALUATION
# ============================================================

def evaluate_requirement(
    requirement,
    cv_text
):

    # EXPERIENCE
    experience_result = evaluate_experience(
        requirement,
        cv_text
    )

    if experience_result is not None:
        return experience_result

    # DEGREE
    if is_degree_requirement(
        requirement
    ):

        return evaluate_degree_requirement(
            requirement,
            cv_text
        )

    # DIRECT
    direct_score = direct_or_alias_score(
        requirement,
        cv_text
    )

    if direct_score is not None:

        if direct_score >= 99:
            status = "Matched"
        else:
            status = "Partial"

        return {

            "requirement":
                requirement,

            "score":
                safe_float(
                    direct_score
                ),

            "status":
                status,

            "method":
                "Direct / skill / alias match"

        }

    # SEMANTIC
    semantic_score = best_semantic_similarity(
        requirement,
        cv_text
    )

    if semantic_score >= 75:

        status = "Matched"

    elif semantic_score >= 55:

        status = "Partial"

    else:

        status = "Not Matched"

    return {

        "requirement":
            requirement,

        "score":
            safe_float(
                semantic_score
            ),

        "status":
            status,

        "method":
            "AI semantic match"

    }


# ============================================================
# REQUIREMENTS
# ============================================================

def evaluate_requirements(
    requirements,
    cv_text
):

    results = []

    for requirement in requirements:

        if not requirement:
            continue

        requirement = str(
            requirement
        ).strip()

        if not requirement:
            continue

        result = evaluate_requirement(
            requirement,
            cv_text
        )

        results.append(
            result
        )

    return results


# ============================================================
# REQUIREMENT SCORE
# ============================================================

def calculate_requirement_score(
    results
):

    if not results:
        return 0.0

    total = sum(

        safe_float(
            item.get(
                "score",
                0
            )
        )

        for item in results

    )

    return round(

        total /
        len(results),

        2

    )


# ============================================================
# CV SECTION DETECTION
# ============================================================

def detect_cv_sections(cv_text):

    text = normalize_text(
        cv_text
    )

    sections = {

        "summary": False,

        "experience": False,

        "education": False,

        "skills": False,

        "projects": False,

        "certifications": False,

        "languages": False,

        "achievements": False

    }

    section_aliases = {

        "summary": [
            "summary",
            "profile",
            "professional summary",
            "career objective",
            "objective"
        ],

        "experience": [
            "experience",
            "work experience",
            "professional experience",
            "employment history"
        ],

        "education": [
            "education",
            "academic background",
            "qualifications"
        ],

        "skills": [
            "skills",
            "technical skills",
            "core skills",
            "competencies"
        ],

        "projects": [
            "projects",
            "academic projects",
            "personal projects"
        ],

        "certifications": [
            "certifications",
            "certificates",
            "licenses"
        ],

        "languages": [
            "languages",
            "language skills"
        ],

        "achievements": [
            "achievements",
            "awards",
            "accomplishments"
        ]

    }

    for section, aliases in section_aliases.items():

        for alias in aliases:

            pattern = (
                r"(?m)^\s*"
                + re.escape(alias)
                + r"\s*:?\s*$"
            )

            if re.search(
                pattern,
                text
            ):

                sections[section] = True
                break

    return sections


# ============================================================
# CV COMPLETENESS
# ============================================================

def analyze_cv_completeness(
    cv_text
):

    sections = detect_cv_sections(
        cv_text
    )

    important_sections = [

        "summary",
        "experience",
        "education",
        "skills"

    ]

    optional_sections = [

        "projects",
        "certifications",
        "languages",
        "achievements"

    ]

    important_score = sum(

        1

        for section
        in important_sections

        if sections[section]

    )

    optional_score = sum(

        1

        for section
        in optional_sections

        if sections[section]

    )

    score = (

        (
            important_score
            /
            len(important_sections)
        )
        * 70

    ) + (

        (
            optional_score
            /
            len(optional_sections)
        )
        * 30

    )

    missing_sections = [

        section.title()

        for section
        in important_sections

        if not sections[section]

    ]

    return {

        "score":
            round(score, 2),

        "sections":
            sections,

        "missing_sections":
            missing_sections

    }


# ============================================================
# PROFESSIONAL TONE
# ============================================================

def analyze_professional_tone(
    cv_text
):

    if not cv_text:

        return {

            "score": 0.0,

            "status":
                "No CV text"

        }

    text = normalize_text(
        cv_text
    )

    score = 100.0
    issues = []

    # Excessive first person
    first_person_words = re.findall(

        r"\b(i|me|my|mine|myself)\b",

        text

    )

    word_count = len(
        text.split()
    )

    if word_count > 0:

        first_person_ratio = (

            len(first_person_words)
            /
            word_count

        )

        if first_person_ratio > 0.025:

            score -= 15

            issues.append(
                "Frequent first-person wording"
            )

    # Informal expressions
    informal_words = [

        "awesome",
        "cool",
        "stuff",
        "things",
        "really good",
        "super",
        "wanna",
        "gonna",
        "etc etc"

    ]

    informal_found = []

    for phrase in informal_words:

        if phrase in text:

            informal_found.append(
                phrase
            )

    if informal_found:

        score -= min(
            25,
            len(informal_found) * 5
        )

        issues.append(
            "Informal wording detected"
        )

    # Excessive exclamation
    exclamation_count = cv_text.count("!")

    if exclamation_count >= 5:

        score -= 10

        issues.append(
            "Excessive exclamation marks"
        )

    # ALL CAPS
    uppercase_words = re.findall(

        r"\b[A-Z]{4,}\b",

        cv_text

    )

    if len(uppercase_words) > 10:

        score -= 10

        issues.append(
            "Excessive uppercase wording"
        )

    score = max(
        0,
        min(
            score,
            100
        )
    )

    if score >= 85:

        status = "Professional"

    elif score >= 70:

        status = "Mostly Professional"

    else:

        status = "Needs Improvement"

    return {

        "score":
            round(score, 2),

        "status":
            status,

        "issues":
            issues

    }


# ============================================================
# WRITING QUALITY
# ============================================================

def analyze_writing_quality(
    cv_text
):

    if not cv_text:

        return {

            "score": 0.0,

            "status":
                "No CV text",

            "issues": []

        }

    sentences = re.split(
        r"[.!?]+",
        cv_text
    )

    sentences = [

        s.strip()

        for s in sentences

        if s.strip()

    ]

    word_count = len(
        cv_text.split()
    )

    score = 100.0
    issues = []

    # Very long sentences
    long_sentences = 0

    for sentence in sentences:

        if len(
            sentence.split()
        ) > 40:

            long_sentences += 1

    if long_sentences > 0:

        score -= min(
            20,
            long_sentences * 4
        )

        issues.append(
            "Some sentences are unusually long"
        )

    # Repeated words
    words = re.findall(
        r"\b[a-zA-Z]{3,}\b",
        cv_text.lower()
    )

    counts = Counter(words)

    repeated_words = [

        word

        for word, count
        in counts.items()

        if count >= 12

    ]

    if repeated_words:

        score -= 10

        issues.append(
            "Some words are heavily repeated"
        )

    # Very short CV
    if word_count < 100:

        score -= 25

        issues.append(
            "CV contains very little text"
        )

    score = max(
        0,
        min(
            score,
            100
        )
    )

    if score >= 85:

        status = "Good"

    elif score >= 70:

        status = "Acceptable"

    else:

        status = "Needs Improvement"

    return {

        "score":
            round(score, 2),

        "status":
            status,

        "issues":
            issues

    }


# ============================================================
# KEYWORD STUFFING
# ============================================================

def analyze_keyword_stuffing(
    cv_text
):

    if not cv_text:

        return {

            "detected": False,

            "score": 100.0,

            "repeated_keywords": []

        }

    found_skills = skills_found(
        cv_text
    )

    normalized = normalize_text(
        cv_text
    )

    repeated_keywords = []

    for skill in found_skills:

        aliases = SKILL_ALIASES.get(
            skill,
            [skill]
        )

        count = 0

        for alias in aliases:

            pattern = (
                r"(?<!\w)"
                + re.escape(
                    normalize_text(alias)
                )
                + r"(?!\w)"
            )

            count += len(
                re.findall(
                    pattern,
                    normalized
                )
            )

        # A skill mentioned excessively
        if count >= 12:

            repeated_keywords.append({

                "keyword":
                    skill,

                "count":
                    count

            })

    detected = (
        len(repeated_keywords) > 0
    )

    if detected:

        score = max(
            50.0,
            100.0 -
            (
                len(repeated_keywords)
                * 10
            )
        )

    else:

        score = 100.0

    return {

        "detected":
            detected,

        "score":
            round(score, 2),

        "repeated_keywords":
            repeated_keywords

    }


# ============================================================
# EXPERIENCE CONSISTENCY
# ============================================================

def analyze_experience_consistency(
    cv_text
):

    periods = extract_experience_periods(
        cv_text
    )

    stated_years_matches = re.findall(

        r"(\d+(?:\.\d+)?)\s*\+?\s*years?",

        extract_experience_section(
            cv_text
        ).lower()

    )

    stated_years = []

    for value in stated_years_matches:

        try:

            stated_years.append(
                float(value)
            )

        except ValueError:
            pass

    detected_years = extract_years_of_experience(
        cv_text
    )

    warnings = []
    score = 100.0

    # Future dates
    current_total = (
        datetime.now().year * 12
        +
        datetime.now().month
    )

    for start, end in periods:

        if start > current_total:

            warnings.append(
                "Future employment date detected"
            )

            score -= 20

        if end > current_total:

            # Current jobs are represented by present
            # and are valid.
            pass

    # Stated years vs detected years
    if stated_years:

        maximum_stated = max(
            stated_years
        )

        if detected_years > 0:

            difference = abs(
                maximum_stated
                -
                detected_years
            )

            if difference >= 2:

                warnings.append(

                    "Stated experience differs "
                    "significantly from detected "
                    "employment dates"

                )

                score -= 25

            elif difference >= 1:

                warnings.append(

                    "Stated experience differs "
                    "somewhat from detected "
                    "employment dates"

                )

                score -= 10

    score = max(
        0,
        min(
            score,
            100
        )
    )

    if score >= 90:

        status = "Consistent"

    elif score >= 70:

        status = "Minor concerns"

    else:

        status = "Potential inconsistency"

    return {

        "score":
            round(score, 2),

        "status":
            status,

        "detected_years":
            safe_float(
                detected_years
            ),

        "stated_years":
            stated_years,

        "warnings":
            warnings

    }


# ============================================================
# CV QUALITY ANALYSIS
# ============================================================

def analyze_cv_quality(
    cv_text
):

    completeness = analyze_cv_completeness(
        cv_text
    )

    tone = analyze_professional_tone(
        cv_text
    )

    writing = analyze_writing_quality(
        cv_text
    )

    keyword_stuffing = analyze_keyword_stuffing(
        cv_text
    )

    experience_consistency = (
        analyze_experience_consistency(
            cv_text
        )
    )

    quality_score = (

        completeness["score"] * 0.30

        +

        tone["score"] * 0.20

        +

        writing["score"] * 0.20

        +

        keyword_stuffing["score"] * 0.10

        +

        experience_consistency["score"] * 0.20

    )

    quality_score = round(
        quality_score,
        2
    )

    if quality_score >= 85:

        status = "Excellent"

    elif quality_score >= 70:

        status = "Good"

    elif quality_score >= 55:

        status = "Fair"

    else:

        status = "Needs Improvement"

    return {

        "score":
            quality_score,

        "status":
            status,

        "completeness":
            completeness,

        "professional_tone":
            tone,

        "writing_quality":
            writing,

        "keyword_stuffing":
            keyword_stuffing,

        "experience_consistency":
            experience_consistency

    }


# ============================================================
# JOB MATCH
# ============================================================

def calculate_job_match(
    job_title,
    job_description,
    cv_text
):

    title_score = 0.0

    description_score = 0.0

    if job_title:

        title_clean = normalize_text(
            job_title
        )

        cv_clean = normalize_text(
            cv_text
        )

        if title_clean and title_clean in cv_clean:

            title_score = 100.0

        else:

            title_score = best_semantic_similarity(
                job_title,
                cv_text
            )

    if job_description:

        description_score = semantic_similarity(
            job_description,
            cv_text
        )

    # Job title = 30%
    # Description = 70%

    if (
        title_score > 0
        and description_score > 0
    ):

        score = (

            title_score * 0.30

            +

            description_score * 0.70

        )

    elif title_score > 0:

        score = title_score

    else:

        score = description_score

    return {

        "title_score":
            round(
                title_score,
                2
            ),

        "description_score":
            round(
                description_score,
                2
            ),

        "score":
            round(
                score,
                2
            )

    }


# ============================================================
# EDUCATION SCORE
# ============================================================

def calculate_education_score(
    required_results,
    preferred_results
):

    degree_results = [

        item

        for item
        in (
            required_results
            +
            preferred_results
        )

        if "degree" in
        item.get(
            "method",
            ""
        ).lower()

    ]

    if not degree_results:

        return 100.0

    total = sum(

        safe_float(
            item.get(
                "score",
                0
            )
        )

        for item in degree_results

    )

    return round(

        total /
        len(degree_results),

        2

    )


# ============================================================
# REQUIRED REQUIREMENT STATUS
# ============================================================

def calculate_required_status(
    required_results
):

    if not required_results:

        return {

            "all_required_met":
                True,

            "failed_count":
                0,

            "partial_count":
                0

        }

    failed = [

        item

        for item
        in required_results

        if item.get(
            "status"
        ) == "Not Matched"

    ]

    partial = [

        item

        for item
        in required_results

        if item.get(
            "status"
        ) == "Partial"

    ]

    return {

        "all_required_met":
            len(failed) == 0,

        "failed_count":
            len(failed),

        "partial_count":
            len(partial)

    }


# ============================================================
# FINAL SCORE
# ============================================================

def calculate_final_score(
    required_score,
    job_match_score,
    preferred_score,
    education_score,
    required_results,
    has_required,
    has_preferred
):

    # --------------------------------------------------------
    # BASE WEIGHTS
    #
    # Required requirements = 45%
    # Job match             = 25%
    # Preferred             = 10%
    # Education             = 10%
    # Requirement coverage  = 10%
    # --------------------------------------------------------

    required_status = calculate_required_status(
        required_results
    )

    # Required score
    required_component = (
        required_score * 0.45
        if has_required
        else 0
    )

    # Job match
    job_component = (
        job_match_score * 0.25
    )

    # Preferred
    preferred_component = (
        preferred_score * 0.10
        if has_preferred
        else 0
    )

    # Education
    education_component = (
        education_score * 0.10
    )

    # Required coverage
    if has_required:

        matched_count = sum(

            1

            for item
            in required_results

            if item.get(
                "status"
            ) == "Matched"

        )

        coverage_score = (

            matched_count
            /
            len(required_results)

        ) * 100

    else:

        coverage_score = 100.0

    coverage_component = (
        coverage_score * 0.10
    )

    # --------------------------------------------------------
    # NORMALIZE IF REQUIRED/PREFERRED DOES NOT EXIST
    # --------------------------------------------------------

    components = []

    if has_required:

        components.append(
            (
                required_score,
                0.45
            )
        )

    components.append(
        (
            job_match_score,
            0.25
        )
    )

    if has_preferred:

        components.append(
            (
                preferred_score,
                0.10
            )
        )

    components.append(
        (
            education_score,
            0.10
        )
    )

    components.append(
        (
            coverage_score,
            0.10
        )
    )

    total_weight = sum(
        weight
        for _, weight
        in components
    )

    final_score = sum(

        score * weight

        for score, weight
        in components

    ) / total_weight

    # --------------------------------------------------------
    # IMPORTANT:
    # Required failures should have a significant effect.
    # --------------------------------------------------------

    if required_status["failed_count"] > 0:

        failed_ratio = (

            required_status["failed_count"]
            /
            len(required_results)

        )

        # Maximum penalty 30 points
        penalty = min(
            30,
            failed_ratio * 30
        )

        final_score -= penalty

    final_score = max(
        0,
        min(
            final_score,
            100
        )
    )

    return round(
        final_score,
        2
    )


# ============================================================
# RECOMMENDATION
# ============================================================

def get_recommendation(
    final_score,
    required_results
):

    required_status = calculate_required_status(
        required_results
    )

    # A candidate failing several required
    # requirements should not receive High Match.

    if required_status["failed_count"] > 0:

        if final_score >= 65:

            return "Review"

        elif final_score >= 50:

            return "Review"

        else:

            return "Low Match"

    if final_score >= 80:

        return "High Match"

    elif final_score >= 65:

        return "Good Match"

    elif final_score >= 50:

        return "Review"

    else:

        return "Low Match"


# ============================================================
# ANALYZE CANDIDATE
# ============================================================

def analyze_candidate(

    job_title,

    job_description,

    required_requirements,

    preferred_requirements,

    cv_text

):

    try:

        # ----------------------------------------------------
        # CLEAN INPUT
        # ----------------------------------------------------

        job_title = (
            job_title
            or ""
        )

        job_description = (
            job_description
            or ""
        )

        cv_text = (
            cv_text
            or ""
        )

        required_requirements = [

            str(item).strip()

            for item
            in (
                required_requirements
                or []
            )

            if str(item).strip()

        ]

        preferred_requirements = [

            str(item).strip()

            for item
            in (
                preferred_requirements
                or []
            )

            if str(item).strip()

        ]

        # ----------------------------------------------------
        # REQUIRED
        # ----------------------------------------------------

        required_results = evaluate_requirements(

            required_requirements,

            cv_text

        )

        required_score = calculate_requirement_score(

            required_results

        )

        # ----------------------------------------------------
        # PREFERRED
        # ----------------------------------------------------

        preferred_results = evaluate_requirements(

            preferred_requirements,

            cv_text

        )

        preferred_score = calculate_requirement_score(

            preferred_results

        )

        # ----------------------------------------------------
        # JOB MATCH
        # ----------------------------------------------------

        job_match = calculate_job_match(

            job_title,

            job_description,

            cv_text

        )

        job_match_score = job_match["score"]

        # ----------------------------------------------------
        # EDUCATION
        # ----------------------------------------------------

        education_score = calculate_education_score(

            required_results,

            preferred_results

        )

        # ----------------------------------------------------
        # EXPERIENCE
        # ----------------------------------------------------

        candidate_years = (
            extract_years_of_experience(
                cv_text
            )
        )

        # ----------------------------------------------------
        # CV QUALITY
        # ----------------------------------------------------

        cv_quality = analyze_cv_quality(
            cv_text
        )

        # ----------------------------------------------------
        # REQUIRED STATUS
        # ----------------------------------------------------

        required_status = calculate_required_status(

            required_results

        )

        # ----------------------------------------------------
        # FINAL SCORE
        # ----------------------------------------------------

        final_score = calculate_final_score(

            required_score,

            job_match_score,

            preferred_score,

            education_score,

            required_results,

            len(required_results) > 0,

            len(preferred_results) > 0

        )

        # ----------------------------------------------------
        # RECOMMENDATION
        # ----------------------------------------------------

        recommendation = get_recommendation(

            final_score,

            required_results

        )

        # ----------------------------------------------------
        # FINAL RESULT
        # ----------------------------------------------------

        return {

            "success": True,

            # Main scores
            "overall_score":
                safe_float(
                    job_match_score
                ),

            "required_score":
                safe_float(
                    required_score
                ),

            "preferred_score":
                safe_float(
                    preferred_score
                ),

            "education_score":
                safe_float(
                    education_score
                ),

            "final_score":
                safe_float(
                    final_score
                ),

            # Recommendation
            "recommendation":
                recommendation,

            # Required status
            "required_status":
                required_status,

            # Job analysis
            "job_match":
                job_match,

            # Experience
            "experience":
                {

                    "candidate_years":
                        safe_float(
                            candidate_years
                        )

                },

            # Requirement details
            "required_results":
                required_results,

            "preferred_results":
                preferred_results,

            # CV quality
            "cv_quality":
                cv_quality

        }

    except Exception as ex:

        return {

            "success":
                False,

            "error":
                str(ex),

            "overall_score":
                0.0,

            "required_score":
                0.0,

            "preferred_score":
                0.0,

            "education_score":
                0.0,

            "final_score":
                0.0,

            "recommendation":
                "AI Analysis Failed",

            "required_results":
                [],

            "preferred_results":
                [],

            "required_status":
                {

                    "all_required_met":
                        False,

                    "failed_count":
                        0,

                    "partial_count":
                        0

                },

            "job_match":
                {},

            "experience":
                {

                    "candidate_years":
                        0.0

                },

            "cv_quality":
                {}

        }