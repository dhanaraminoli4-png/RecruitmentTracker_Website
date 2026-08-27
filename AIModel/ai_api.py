from flask import Flask, request, jsonify
import os
import tempfile
import inspect
import numpy as np

import ai_engine
import cv_reader


app = Flask(__name__)


# ============================================================
# CONVERT NUMPY VALUES TO NORMAL PYTHON VALUES
# ============================================================

def make_json_safe(value):
    """
    Convert NumPy/Python values into values that Flask
    can safely convert to JSON.
    """

    if isinstance(value, dict):
        return {
            str(key): make_json_safe(val)
            for key, val in value.items()
        }

    if isinstance(value, (list, tuple)):
        return [
            make_json_safe(item)
            for item in value
        ]

    if isinstance(value, np.ndarray):
        return value.tolist()

    if isinstance(value, np.integer):
        return int(value)

    if isinstance(value, np.floating):
        return float(value)

    if isinstance(value, np.bool_):
        return bool(value)

    if isinstance(value, float):
        return float(value)

    if isinstance(value, int):
        return int(value)

    if isinstance(value, bool):
        return bool(value)

    return value


# ============================================================
# CV TEXT EXTRACTION
# ============================================================

def extract_cv_text(file_path):
    """
    Use the CV reader to extract text from the uploaded CV.

    Supports the common function names used by cv_reader.py.
    """

    possible_functions = [
        "extract_text",
        "extract_text_from_file",
        "extract_cv_text",
        "read_cv",
        "read_cv_text"
    ]

    for function_name in possible_functions:

        function = getattr(
            cv_reader,
            function_name,
            None
        )

        if function is None:
            continue

        try:
            return function(file_path)
        except TypeError:
            try:
                with open(
                    file_path,
                    "rb"
                ) as file:

                    return function(file)

            except Exception:
                continue

    raise Exception(
        "Could not find a compatible CV text extraction "
        "function in cv_reader.py."
    )


# ============================================================
# AI ENGINE
# ============================================================

def run_ai_engine(
    cv_text,
    job_title,
    job_description,
    required_requirements,
    preferred_requirements
):
    """
    Call analyze_candidate() from ai_engine.py.

    The argument order exactly matches the current
    analyze_candidate() function in ai_engine.py.
    """

    analyze_function = getattr(
        ai_engine,
        "analyze_candidate",
        None
    )

    if analyze_function is None:
        raise Exception(
            "ai_engine.py does not contain analyze_candidate()."
        )

    print("\n==========================================")
    print("RUNNING AI ENGINE")
    print("==========================================")
    print("Job Title:")
    print(job_title)

    print("\nRequired Requirements:")
    for requirement in required_requirements:
        print("-", requirement)

    print("\nPreferred Requirements:")
    for requirement in preferred_requirements:
        print("-", requirement)

    print("\nCV Text Length:")
    print(len(cv_text))

    # IMPORTANT:
    # These names exactly match ai_engine.py
    result = analyze_function(
        job_title=job_title,
        job_description=job_description,
        required_requirements=required_requirements,
        preferred_requirements=preferred_requirements,
        cv_text=cv_text
    )

    print("\nAI ENGINE COMPLETED")
    print("Result type:", type(result))

    return result

    # --------------------------------------------------------
    # Try to inspect the function parameters
    # --------------------------------------------------------

    try:

        signature = inspect.signature(
            analyze_function
        )

        parameters = signature.parameters

        kwargs = {}

        aliases = {

            "cv_text": cv_text,
            "resume_text": cv_text,
            "candidate_text": cv_text,
            "text": cv_text,

            "job_title": job_title,
            "title": job_title,

            "job_description": job_description,
            "description": job_description,

            "required_requirements":
                required_requirements,

            "required":
                required_requirements,

            "required_skills":
                required_requirements,

            "preferred_requirements":
                preferred_requirements,

            "preferred":
                preferred_requirements,

            "preferred_skills":
                preferred_requirements
        }

        for parameter_name in parameters:

            if parameter_name in aliases:

                kwargs[parameter_name] = \
                    aliases[parameter_name]

        # ----------------------------------------------------
        # If we found matching parameters, use them
        # ----------------------------------------------------

        if kwargs:

            return analyze_function(**kwargs)

    except Exception:
        pass

    # --------------------------------------------------------
    # Fallback to the standard argument order
    # --------------------------------------------------------

    return analyze_function(
        cv_text,
        job_title,
        job_description,
        required_requirements,
        preferred_requirements
    )


# ============================================================
# NORMALISE AI RESULT
# ============================================================

def normalise_result(result):
    """
    Convert different possible result formats from ai_engine.py
    into the format expected by the ASP.NET application.
    """

    result = make_json_safe(result)

    # --------------------------------------------------------
    # Dictionary result
    # --------------------------------------------------------

    if isinstance(result, dict):

        # Different possible names for scores
        overall_score = result.get(
            "overallScore",
            result.get(
                "overall_score",
                result.get(
                    "overall",
                    0
                )
            )
        )

        required_score = result.get(
            "requiredScore",
            result.get(
                "required_score",
                result.get(
                    "required",
                    0
                )
            )
        )

        preferred_score = result.get(
            "preferredScore",
            result.get(
                "preferred_score",
                result.get(
                    "preferred",
                    0
                )
            )
        )

        final_score = result.get(
            "finalScore",
            result.get(
                "final_score",
                result.get(
                    "score",
                    overall_score
                )
            )
        )

        recommendation = result.get(
            "recommendation",
            result.get(
                "assessment",
                "Review"
            )
        )

        return {
            "success": True,

            "overallScore":
                float(overall_score or 0),

            "requiredScore":
                float(required_score or 0),

            "preferredScore":
                float(preferred_score or 0),

            "finalScore":
                float(final_score or 0),

            "recommendation":
                str(recommendation),

            "requiredResults":
                result.get(
                    "requiredResults",
                    result.get(
                        "required_results",
                        []
                    )
                ),

            "preferredResults":
                result.get(
                    "preferredResults",
                    result.get(
                        "preferred_results",
                        []
                    )
                )
        }

    # --------------------------------------------------------
    # If AI engine returns a number
    # --------------------------------------------------------

    if isinstance(
        result,
        (int, float, np.integer, np.floating)
    ):

        score = float(result)

        return {
            "success": True,
            "overallScore": score,
            "requiredScore": score,
            "preferredScore": score,
            "finalScore": score,
            "recommendation": get_recommendation(score),
            "requiredResults": [],
            "preferredResults": []
        }

    raise Exception(
        "Unexpected result returned from ai_engine.py."
    )


# ============================================================
# RECOMMENDATION
# ============================================================

def get_recommendation(score):

    score = float(score)

    if score >= 80:
        return "High Match"

    if score >= 65:
        return "Good Match"

    if score >= 50:
        return "Review"

    return "Low Match"


# ============================================================
# ANALYZE CV API
# ============================================================

@app.route(
    "/analyze",
    methods=["POST"]
)
def analyze():

    temp_file_path = None

    try:

        # ====================================================
        # CHECK CV
        # ====================================================

        if "cv" not in request.files:

            return jsonify({
                "success": False,
                "error": "No CV file was provided."
            }), 400

        cv_file = request.files["cv"]

        if cv_file.filename == "":

            return jsonify({
                "success": False,
                "error": "CV file has no filename."
            }), 400

        # ====================================================
        # GET JOB INFORMATION
        # ====================================================

        job_title = request.form.get(
            "job_title",
            ""
        )

        job_description = request.form.get(
            "job_description",
            ""
        )

        required_string = request.form.get(
            "required_requirements",
            ""
        )

        preferred_string = request.form.get(
            "preferred_requirements",
            ""
        )

        # ====================================================
        # CONVERT REQUIREMENTS TO LISTS
        # ====================================================

        required_requirements = [
            item.strip()
            for item in required_string.split("|")
            if item.strip()
        ]

        preferred_requirements = [
            item.strip()
            for item in preferred_string.split("|")
            if item.strip()
        ]

        # ====================================================
        # SAVE TEMPORARY CV
        # ====================================================

        extension = os.path.splitext(
            cv_file.filename
        )[1].lower()

        with tempfile.NamedTemporaryFile(
            delete=False,
            suffix=extension
        ) as temp_file:

            cv_file.save(
                temp_file.name
            )

            temp_file_path = temp_file.name

        # ====================================================
        # EXTRACT CV TEXT
        # ====================================================

        cv_text = extract_cv_text(
            temp_file_path
        )

        if cv_text is None:

            raise Exception(
                "CV text extraction returned no result."
            )

        cv_text = str(cv_text)

        if not cv_text.strip():

            raise Exception(
                "No readable text could be extracted "
                "from the CV."
            )

        # ====================================================
        # RUN AI
        # ====================================================

        result = run_ai_engine(

            cv_text=cv_text,

            job_title=job_title,

            job_description=job_description,

            required_requirements=
                required_requirements,

            preferred_requirements=
                preferred_requirements
        )

        # ====================================================
        # NORMALISE RESULT
        # ====================================================

        response = normalise_result(
            result
        )

        # ====================================================
        # FINAL JSON-SAFE CONVERSION
        # ====================================================

        response = make_json_safe(
            response
        )

        return jsonify({
            "success": True,
            "overallScore": float(response.get("overallScore", 0)),
            "requiredScore": float(response.get("requiredScore", 0)),
            "preferredScore": float(response.get("preferredScore", 0)),
            "finalScore": float(response.get("finalScore", 0)),
            "recommendation": str(
                response.get("recommendation", "Review")
            ),

            "requiredResults": response.get(
                "requiredResults", []
            ),

            "preferredResults": response.get(
                "preferredResults", []
            )
        }), 200

    except Exception as ex:

        print(
            "AI API ERROR:",
            str(ex)
        )

        return jsonify({
            "success": False,
            "error": str(ex)
        }), 500

    finally:

        # ====================================================
        # DELETE TEMPORARY FILE
        # ====================================================

        if temp_file_path is not None:

            try:

                if os.path.exists(
                    temp_file_path
                ):
                    os.remove(
                        temp_file_path
                    )

            except Exception:
                pass


# ============================================================
# HEALTH CHECK
# ============================================================

@app.route(
    "/",
    methods=["GET"]
)
def home():

    return jsonify({
        "success": True,
        "message": "Recruitment AI API is running."
    })


# ============================================================
# RUN SERVER
# ============================================================

if __name__ == "__main__":

    print(
        "=========================================="
    )

    print(
        " RecruitmentTracker AI API"
    )

    print(
        " Running on http://127.0.0.1:5000"
    )

    print(
        "=========================================="
    )

    app.run(
        host="127.0.0.1",
        port=5000,
        debug=True
    )