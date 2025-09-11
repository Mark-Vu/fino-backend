resource "aws_s3_bucket" "bank_statement_converter" {
  bucket = "${var.project_name}-bucket-production"

  tags = {
    Project = var.project_name
  }
}

# Block public access (but allow bucket policy for public-access folder)
resource "aws_s3_bucket_public_access_block" "bank_statement_converter" {
  bucket = aws_s3_bucket.bank_statement_converter.id

  block_public_acls       = true
  block_public_policy     = false  # Allow bucket policy for public-access folder
  ignore_public_acls      = true
  restrict_public_buckets = false  # Allow public access via bucket policy
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
      "https://api.finotools.app",
      "https://*.finotools.app"
    ]
    expose_headers  = ["ETag", "Content-Length"]
    max_age_seconds = 3000
  }
}

# Bucket policy to allow public read access only to public-access/ folder
resource "aws_s3_bucket_policy" "bank_statement_converter" {
  bucket = aws_s3_bucket.bank_statement_converter.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "PublicReadGetObject"
        Effect    = "Allow"
        Principal = "*"
        Action    = "s3:GetObject"
        Resource  = "${aws_s3_bucket.bank_statement_converter.arn}/public-access/*"
      }
    ]
  })

  depends_on = [aws_s3_bucket_public_access_block.bank_statement_converter]
}

# Create a placeholder object to establish the public-access folder
resource "aws_s3_object" "public_access_folder" {
  bucket = aws_s3_bucket.bank_statement_converter.id
  key    = "public-access/.keep"
  content = "This file ensures the public-access folder exists"
  
  tags = {
    Project = var.project_name
    Purpose = "Folder placeholder"
  }
}
