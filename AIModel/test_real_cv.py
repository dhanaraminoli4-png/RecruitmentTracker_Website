
from cv_reader import extract_cv_text

# Import your existing AI functions
from test_matching import analyze_candidate


# ============================================================
# REAL CV PATH
# ============================================================

cv_path = r"C:\Users\VICTUS\source\repos\RecruitmentTracker\RecruitmentTracker\AIModel\Test_1_cv.pdf"


# ============================================================
# JOB VACANCY
# ============================================================

job_title = "Junior Data Analyst"


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
# EXTRACT REAL CV
# ============================================================

print()
print("=" * 75)
print("READING REAL CV")
print("=" * 75)


cv_text = extract_cv_text(
    cv_path
)


if not cv_text.strip():

    print("ERROR: No text could be extracted from the CV.")

    exit()


print("CV successfully extracted.")
print()


# ============================================================
# RUN AI
# ============================================================

result = analyze_candidate(

    job_title,

    job_description,

    required_requirements,

    preferred_requirements,

    cv_text

)


# ============================================================
# DISPLAY RESULT
# ============================================================

print("=" * 75)
print("AI CANDIDATE ANALYSIS")
print("=" * 75)


print()

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
print("=" * 75)
print("REQUIRED REQUIREMENTS")
print("=" * 75)


for item in result["required_results"]:

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
print("=" * 75)
print("PREFERRED REQUIREMENTS")
print("=" * 75)


for item in result["preferred_results"]:

    print(

        f"[{item['status']}] "
        f"{item['requirement']} "
        f"({item['score']}%) "

        f"- {item['method']}"

    )


print()
print("=" * 75)
print("AI ANALYSIS COMPLETE")
print("=" * 75)
