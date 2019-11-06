get_parameters <- function() {

  # noise

  # 1st param
  p1 <- 1 # [u]

  # noise

  # 2nd param
  p2 <- 2 # v

  # noise

  return(list(p1 = p1, p2 = p2))
}

parameters <- get_parameters()

run <- function(parameters) {

  i1 <- rep_len(parameters$p1, 10)
  i2 <- rep_len(parameters$p2, 10)

  ## noise 
  # 1st output
  o1 <- i1 + i2 # u

  ## noise 
  # 2nd output
  o2 <- i1 * i2 # [u.v]

  ## noise 

  # fake diff
  Ro3 <- i1 ** i2 # local uses some prefix [u^v]

  return(list(o1 = o1, o2 = o2, o3 = Ro3))
}

o <- run(parameters)
