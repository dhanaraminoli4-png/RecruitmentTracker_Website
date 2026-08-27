import os
from PyPDF2 import PdfReader
from docx import Document


def extract_text_from_pdf(file_path):

    text = ""

    reader = PdfReader(file_path)

    for page in reader.pages:

        page_text = page.extract_text()

        if page_text:
            text += page_text + "\n"

    return text


def extract_text_from_docx(file_path):

    text = ""

    document = Document(file_path)

    for paragraph in document.paragraphs:

        if paragraph.text.strip():

            text += paragraph.text + "\n"

    return text


def extract_cv_text(file_path):

    extension = os.path.splitext(
        file_path
    )[1].lower()


    if extension == ".pdf":

        return extract_text_from_pdf(
            file_path
        )


    elif extension == ".docx":

        return extract_text_from_docx(
            file_path
        )


    else:

        raise ValueError(
            "Only PDF and DOCX files are supported."
        )
