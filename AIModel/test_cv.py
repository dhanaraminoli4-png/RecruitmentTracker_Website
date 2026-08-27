from cv_reader import extract_cv_text


cv_path = r"C:\Users\VICTUS\source\repos\RecruitmentTracker\RecruitmentTracker\AIModel\Test_1_cv.pdf"


print()
print("=" * 70)
print("READING CV")
print("=" * 70)


cv_text = extract_cv_text(cv_path)


print()
print("EXTRACTED CV TEXT")
print("=" * 70)

print(cv_text)

print("=" * 70)
print("END")
print("=" * 70)
