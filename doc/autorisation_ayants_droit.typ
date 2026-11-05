// Autorisation des ayants droit de Michel Chéenne (voir NOTICE).
// PDF à imprimer : typst compile doc/autorisation_ayants_droit.typ
//   (ou : nix run nixpkgs#typst -- compile doc/autorisation_ayants_droit.typ)

#set document(title: "Autorisation de diffusion — Trampoline")
#set page(paper: "a4", margin: (x: 2.5cm, y: 2.5cm))
#set text(lang: "fr", size: 12pt)
#set par(justify: true, leading: 0.8em)
#show table: set par(justify: false)

#align(center)[
  #text(size: 18pt, weight: "bold")[Autorisation de diffusion]
  #v(0.2em)
  #text(size: 13pt)[« Trampoline – un jeu pour faire rebondir les mots »]
]

#v(2em)

Nous, soussignés, ayants droit de Michel Chéenne, auteur du jeu
« Trampoline – un jeu pour faire rebondir les mots », autorisons
Maximilien Naveau à diffuser les grilles de mots croisés et les documents
de règles créés par Michel Chéenne sous licence Creative Commons
Attribution 4.0 (CC BY 4.0), y compris dans des versions commerciales du
jeu, à condition que Michel Chéenne soit toujours crédité comme auteur.

#v(2.5em)

Fait à #box(width: 6cm, repeat[.]), le #box(width: 4cm, repeat[.])

#v(2em)

Noms et signatures :

#v(0.8em)

#table(
  columns: (1.6fr, 1.2fr, 1.2fr),
  rows: (auto, 2.6cm, 2.6cm, 2.6cm, 2.6cm),
  inset: 8pt,
  align: (left + top),
  table.header(
    [*Nom et prénom*], [*Lien avec Michel Chéenne*], [*Signature*],
  ),
  [], [], [],
  [], [], [],
  [], [], [],
  [], [], [],
)
