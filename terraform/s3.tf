resource "aws_s3_bucket" "bank_statement_converter" {
  bucket = "${var.project_name}-bucket-production"

  tags = {
    Project = var.project_name
  }
}

# Block public access
resource "aws_s3_bucket_public_access_block" "bank_statement_converter" {
  bucket = aws_s3_bucket.bank_statement_converter.id

  block_public_acls   = true
  block_public_policy = true
  ignore_public_acls  = true
  restrict_public_buckets = true
}

# Enable versioning (optional, keeps old files)
resource "aws_s3_bucket_versioning" "bank_statement_converter" {
  bucket = aws_s3_bucket.bank_statement_converter.id
  versioning_configuration {
    status = "Enabled"
  }
}

# Server-side encryption
resource "aws_s3_bucket_server_side_encryption_configuration" "bank_statement_converter" {
  bucket = aws_s3_bucket.bank_statement_converter.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_cors_configuration" "bank_statement_converter" {
  bucket = aws_s3_bucket.bank_statement_converter.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT", "POST", "DELETE", "HEAD"]
    allowed_origins = [
      "http://localhost:3000",
      "https://finotools.app",
      "https://api.finotools.app"
    ]
    expose_headers  = ["ETag", "Content-Length"]
    max_age_seconds = 3000
  }
}
