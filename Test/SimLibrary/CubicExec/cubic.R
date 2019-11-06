do_cubic <- function(params) {
  a <- params[["a"]]
  b <- params[["b"]]
  c <- params[["c"]]
  d <- params[["d"]]

  x <- params[["x"]]

  y <- a * x ^ 3 + b * x ^ 2 + c * x + d

  res <- list(x = x, y = y)

  return(res)
}

params <- list(
  a = 3,
  b = -12,
  c = 0,
  d = 50,
  x = seq(0, 4, by = 0.4)
  )

res <- do_cubic(params)
