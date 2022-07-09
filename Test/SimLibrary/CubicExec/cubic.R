assign_parameters <- function() {
  # BEGIN PARAMETERS
  a <- 3 # []
  b <- -12 # []
  c <- 0 # []
  d <- 50 # []
  # END PARAMETERS
}

run_model <- function(params) {
  a <- params[["a"]]
  b <- params[["b"]]
  c <- params[["c"]]
  d <- params[["d"]]

  # BEGIN OUTPUTS
  x <<- seq(0, 4, by = 0.4) # []
  y <<- a * x ^ 3 + b * x ^ 2 + c * x + d # []
  # END OUTPUTS
}
