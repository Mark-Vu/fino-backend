# IAM policy that allows Textract (async) + S3 read/write on your existing bucket
data "aws_iam_policy_document" "textract_policy_doc" {
  statement {
    sid     = "TextractDocumentAnalysis"
    effect  = "Allow"
    actions = [
      "textract:StartDocumentAnalysis",
      "textract:GetDocumentAnalysis"
    ]
    resources = ["*"]
  }

  # Read PDFs
  statement {
    sid       = "S3ReadPDFs"
    effect    = "Allow"
    actions   = ["s3:GetObject"]
    resources = ["arn:aws:s3:::${aws_s3_bucket.bank_statement_converter.bucket}/*"]
  }

  # Write CSV results (same bucket; keep if you upload CSVs there)
  statement {
    sid       = "S3WriteCSV"
    effect    = "Allow"
    actions   = ["s3:PutObject"]
    resources = ["arn:aws:s3:::${aws_s3_bucket.bank_statement_converter.bucket}/*"]
  }
}

resource "aws_iam_policy" "textract_policy" {
  name        = "${var.project_name}-textract-rw"
  description = "Allow Textract (Start/Get) and S3 read/write for PDFs/CSV"
  policy      = data.aws_iam_policy_document.textract_policy_doc.json
}

# Attach to IAM user (local/dev) — your logs show arn:...:user/admin
resource "aws_iam_user_policy_attachment" "attach_to_user" {
  user       = var.iam_user_name
  policy_arn = aws_iam_policy.textract_policy.arn
}
