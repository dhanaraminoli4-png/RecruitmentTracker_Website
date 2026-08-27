from sentence_transformers import SentenceTransformer
from sklearn.metrics.pairwise import cosine_similarity
import re


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

    text = text.lower()

    text = text.replace("&", " and ")

    text = re.sub(
        r"[^a-z0-9+#.\s]",
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
# SKILL / REQUIREMENT ALIASES
# ============================================================

ALIASES = {

    "microsoft excel": [
        "excel",
        "ms excel",
        "microsoft excel",
        "microsoft office excel",
        "spreadsheet",
        "spreadsheets",
        "spreadsheet analysis"
    ],

    "python": [
        "python",
        "python programming",
        "python programming language"
    ],

    "sql": [
        "sql",
        "structured query language",
        "mysql",
        "microsoft sql",
        "sql server"
    ],

    "power bi": [
        "power bi",
        "powerbi",
        "microsoft power bi"
    ],

    "tableau": [
        "tableau"
    ],

    "machine learning": [
        "machine learning",
        "machine-learning",
        "ml"
    ],

    "data analysis": [
        "data analysis",
        "data analytics",
        "data analyst",
        "analytical analysis",
        "business analysis"
    ],

    "data visualization": [
        "data visualization",
        "data visualisation",
        "data viz",
        "visualization",
        "visualisation",
        "data dashboards",
        "dashboards"
    ],

    "statistics": [
        "statistics",
        "statistical analysis",
        "statistical modelling",
        "statistical modeling"
    ],

    "bachelor's degree": [
        "bachelor's degree",
        "bachelors degree",
        "bachelor degree",
        "bachelor",
        "bsc",
        "b.sc",
        "b.sc.",
        "bsc degree",
        "bachelor of science",
        "ba",
        "b.a",
        "b.a.",
        "bachelor of arts",
        "beng",
        "b.eng",
        "b.eng.",
        "bachelor of engineering",
        "be",
        "b.e",
        "b.e."
    ]
}


# ============================================================
# GET ALIASES
# ============================================================

def get_aliases(requirement):

    requirement_clean = normalize_text(
        requirement
    )

    for key, aliases in ALIASES.items():

        if (
            requirement_clean == key
            or
            requirement_clean in aliases
        ):

            return aliases

    return [
        requirement_clean
    ]


# ============================================================
# ALIAS MATCH
# ============================================================

def alias_match(
    requirement,
    cv_text
):

    cv_clean = normalize_text(
        cv_text
    )

    aliases = get_aliases(
        requirement
    )

    for alias in aliases:

        alias_clean = normalize_text(
            alias
        )

        if not alias_clean:
            continue

        pattern = (
            r"\b"
            +
            re.escape(alias_clean)
            +
            r"\b"
        )

        if re.search(
            pattern,
            cv_clean
        ):

            return True

    return False


# ============================================================
# EXTRACT EXPERIENCE FROM CV
# ============================================================

def extract_years_of_experience(cv_text):

    text = normalize_text(
        cv_text
    )

    numbers = []

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

    # Numeric years

    numeric_matches = re.findall(

        r"(\d+(?:\.\d+)?)\s*\+?\s*years?",

        text

    )

    for value in numeric_matches:

        try:

            numbers.append(
                float(value)
            )

        except ValueError:

            pass


    # Written years

    for word, value in word_numbers.items():

        pattern = (

            r"\b"
            +
            word
            +
            r"\s*(?:\+)?\s*years?\b"

        )

        if re.search(
            pattern,
            text
        ):

            numbers.append(
                float(value)
            )


    if not numbers:

        return 0


    return max(numbers)


# ============================================================
# EXTRACT REQUIRED EXPERIENCE
# ============================================================

def extract_required_experience(
    requirement
):

    text = normalize_text(
        requirement
    )

    patterns = [

        r"at least\s+(\d+(?:\.\d+)?)\s*years?",

        r"minimum\s+(\d+(?:\.\d+)?)\s*years?",

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

    return None


# ============================================================
# EXPERIENCE EVALUATION
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

    candidate_years = (
        extract_years_of_experience(
            cv_text
        )
    )

    if required_years is None:

        return None


    if candidate_years >= required_years:

        return {

            "requirement":
                requirement,

            "score":
                100.0,

            "status":
                "Matched",

            "method":
                "Experience comparison",

            "required_years":
                required_years,

            "candidate_years":
                candidate_years

        }


    elif candidate_years > 0:

        percentage = (

            candidate_years
            /
            required_years

        ) * 100

        return {

            "requirement":
                requirement,

            "score":
                round(
                    min(
                        percentage,
                        99
                    ),
                    2
                ),

            "status":
                "Partial",

            "method":
                "Experience comparison",

            "required_years":
                required_years,

            "candidate_years":
                candidate_years

        }


    return {

        "requirement":
            requirement,

        "score":
            0.0,

        "status":
            "Not Matched",

        "method":
            "Experience comparison",

        "required_years":
            required_years,

        "candidate_years":
            0

    }


# ============================================================
# DIRECT MATCH
# ============================================================

def direct_match(
    requirement,
    cv_text
):

    requirement_clean = normalize_text(
        requirement
    )

    cv_clean = normalize_text(
        cv_text
    )


    # Exact requirement

    pattern = (
        r"\b"
        +
        re.escape(requirement_clean)
        +
        r"\b"
    )

    if re.search(
        pattern,
        cv_clean
    ):

        return True


    # Alias matching

    if alias_match(
        requirement,
        cv_text
    ):

        return True


    # Word-based matching

    requirement_words = set(
        requirement_clean.split()
    )

    cv_words = set(
        cv_clean.split()
    )

    if not requirement_words:

        return False


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


    return word_ratio >= 0.70


# ============================================================
# AI SEMANTIC SIMILARITY
# ============================================================

def semantic_similarity(
    requirement,
    cv_text
):

    embeddings = model.encode(

        [
            requirement,
            cv_text
        ],

        normalize_embeddings=True

    )

    similarity = cosine_similarity(

        [embeddings[0]],

        [embeddings[1]]

    )[0][0]


    return max(

        0,

        min(
            similarity * 100,
            100
        )

    )


# ============================================================
# EVALUATE REQUIREMENT
# ============================================================

def evaluate_requirement(
    requirement,
    cv_text
):

    # --------------------------------------------------------
    # EXPERIENCE
    # --------------------------------------------------------

    experience_result = evaluate_experience(

        requirement,

        cv_text

    )

    if experience_result is not None:

        return experience_result


    # --------------------------------------------------------
    # DIRECT / ALIAS MATCH
    # --------------------------------------------------------

    if direct_match(

        requirement,

        cv_text

    ):

        return {

            "requirement":
                requirement,

            "score":
                100.0,

            "status":
                "Matched",

            "method":
                "Direct / alias match"

        }


    # --------------------------------------------------------
    # SEMANTIC AI MATCH
    # --------------------------------------------------------

    semantic_score = semantic_similarity(

        requirement,

        cv_text

    )


    if semantic_score >= 65:

        status = "Matched"

    elif semantic_score >= 45:

        status = "Partial"

    else:

        status = "Not Matched"


    return {

        "requirement":
            requirement,

        "score":
            round(
                semantic_score,
                2
            ),

        "status":
            status,

        "method":
            "AI semantic match"

    }


# ============================================================
# EVALUATE REQUIREMENTS
# ============================================================

def evaluate_requirements(
    requirements,
    cv_text
):

    results = []

    for requirement in requirements:

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

        return 0


    total = sum(

        item["score"]

        for item in results

    )


    return total / len(results)


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

    required_results = evaluate_requirements(

        required_requirements,

        cv_text

    )


    required_score = calculate_requirement_score(

        required_results

    )


    preferred_results = evaluate_requirements(

        preferred_requirements,

        cv_text

    )


    preferred_score = calculate_requirement_score(

        preferred_results

    )


    job_text = f"""

    Job Title:
    {job_title}

    Job Description:
    {job_description}

    Required Requirements:
    {", ".join(required_requirements)}

    Preferred Requirements:
    {", ".join(preferred_requirements)}

    """


    overall_score = semantic_similarity(

        job_text,

        cv_text

    )


    # --------------------------------------------------------
    # FINAL SCORE
    #
    # Required = 60%
    # Job Match = 25%
    # Preferred = 15%
    # --------------------------------------------------------

    final_score = (

        required_score * 0.60

        +

        overall_score * 0.25

        +

        preferred_score * 0.15

    )


    final_score = round(

        final_score,

        2

    )


    if final_score >= 80:

        recommendation = "High Match"

    elif final_score >= 65:

        recommendation = "Good Match"

    elif final_score >= 50:

        recommendation = "Review"

    else:

        recommendation = "Low Match"


    return {

        "overall_score":
            round(
                overall_score,
                2
            ),

        "required_score":
            round(
                required_score,
                2
            ),

        "preferred_score":
            round(
                preferred_score,
                2
            ),

        "final_score":
            final_score,

        "recommendation":
            recommendation,

        "required_results":
            required_results,

        "preferred_results":
            preferred_results

    }


# ============================================================
# TEST JOB
# ============================================================

job_title = """
Junior Data Analyst
"""


job_description = """
We are looking for a Junior Data Analyst to analyse
business data, create reports and dashboards, and
support data-driven decision making.

The candidate should be comfortable working with
large datasets and communicating analytical findings.
"""


required_requirements = [

    "Python",

    "SQL",

    "Microsoft Excel",

    "Data analysis",

    "Data visualization",

    "Bachelor's degree",

    "At least 1 year of relevant experience"

]


preferred_requirements = [

    "Power BI",

    "Tableau",

    "Statistics",

    "Machine learning"

]


# ============================================================
# TEST CANDIDATES
# ============================================================

candidates = {

    "Candidate A": """

    Bachelor's degree in Data Science.

    Two years of experience working as a Data Analyst.

    Strong experience with Python, SQL and Microsoft Excel.

    Created Power BI dashboards and business reports.

    Experienced in data cleaning, data analysis and
    data visualization.

    """,

    "Candidate B": """

    Bachelor's degree in Computer Science.

    Three years of experience as a Software Developer.

    Strong experience with Java, C#, ASP.NET and SQL.

    Developed enterprise web applications.

    Some experience working with databases.

    """,

    "Candidate C": """

    Bachelor's degree in Accounting.

    Four years of experience working in accounting.

    Experienced with Microsoft Excel, financial reporting
    and QuickBooks.

    Responsible for preparing financial statements.

    """,

    "Candidate D": """

    Bachelor's degree in Marketing.

    Two years of experience in digital marketing.

    Experienced with SEO, Google Ads, social media
    marketing and content creation.

    """

}


# ============================================================
# RUN AI TEST
# ============================================================

if __name__ == "__main__":

    all_results = []


    for candidate_name, cv_text in candidates.items():

        result = analyze_candidate(

            job_title,

            job_description,

            required_requirements,

            preferred_requirements,

            cv_text

        )


        all_results.append({

            "candidate":
                candidate_name,

            **result

        })


    # ========================================================
    # RANK
    # ========================================================

    all_results.sort(

        key=lambda x:
            x["final_score"],

        reverse=True

    )


    # ========================================================
    # DISPLAY RESULTS
    # ========================================================

    print()

    print("=" * 75)

    print(
        "AI RECRUITMENT CANDIDATE RANKING"
    )

    print("=" * 75)


    for rank, result in enumerate(

        all_results,

        start=1

    ):

        print()

        print(

            f"RANK #{rank}: "
            f"{result['candidate']}"

        )

        print("-" * 75)


        print(

            f"Overall Job Match: "
            f"{result['overall_score']}%"

        )


        print(

            f"Required Requirements Score: "
            f"{result['required_score']}%"

        )


        print(

            f"Preferred Requirements Score: "
            f"{result['preferred_score']}%"

        )


        print(

            f"FINAL AI SCORE: "
            f"{result['final_score']}%"

        )


        print(

            f"AI Assessment: "
            f"{result['recommendation']}"

        )


        print()

        print(
            "REQUIRED REQUIREMENTS"
        )


        for item in result[
            "required_results"
        ]:

            if item["method"] == "Experience comparison":

                print(

                    f"[{item['status']}] "
                    f"{item['requirement']} "
                    f"({item['score']}%) "
                    f"- Required: "
                    f"{item['required_years']} years, "
                    f"Candidate: "
                    f"{item['candidate_years']} years"

                )

            else:

                print(

                    f"[{item['status']}] "
                    f"{item['requirement']} "
                    f"({item['score']}%) "
                    f"- {item['method']}"

                )


        print()

        print(
            "PREFERRED REQUIREMENTS"
        )


        for item in result[
            "preferred_results"
        ]:

            print(

                f"[{item['status']}] "
                f"{item['requirement']} "
                f"({item['score']}%) "
                f"- {item['method']}"

            )


        print()

        print("=" * 75)