# noise

# 1st param
p1 <- 1 # [u]

# noise

# 2nd param
p2 <- 2 # v

# noise

# fake independent var
t <- seq_len(10)

run <- function() {

  # fake sim seqs
  i1 <- rep_len(p1, length(t))
  i2 <- rep_len(p2, length(t))

  ## noise 
  # 1st output
  o1 <- i1 + i2 # u

  ## noise 
  # 2nd output
  o2 <- i1 * i2 # [u.v]

  ## noise 

  # fake diff
  Ro3 <- i1 ** i2 # local uses some prefix [u^v]

  # non-state computation
  # comp
  o4 <- i2 %% i1 # scalar [u/v]

  return(list(o1 = o1, o2 = o2, o3 = Ro3))
}

o <- run()

o4 <- o$o2 %% o$o1
