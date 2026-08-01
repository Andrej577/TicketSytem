2. SignalR za dashboard
3. Redizajn dashboard (date picker)
4. Events on Recent activity na dashboard
5. Mailer funkcionalnost?
6. KB ostaviti kako je ili redizajn (grupe u listi, novi filteri) ?
7. Notification button (mozda maknuti a staviti badge na buttone kada se nesto promjeni tickets/kb/customers)
8. Nove ikone (koje imamo kupljene zaboravio sam gdje)
9. Fix fonta na svim stranicama isti bi trebao biti (1.6 razlika na fontovima, bold gjde je bitno itd itd, računalna grafika)
10. Elevated ili flat izgled sučelja? Uzeti jedan i napraviti cjeli app konzistentnim
11. Ubaciti ovu ikonu za probu
    <svg viewBox="0 -16 512 512" xmlns="http://www.w3.org/2000/svg" id="fi_1063366"><path d="m467 0h-422c-24.8125 0-45 20.1875-45 45v300c0 24.8125 20.1875 45 45 45h130.1875l-20 60h-49.1875c-8.285156 0-15 6.714844-15 15s6.714844 15 15 15h300c8.285156 0 15-6.714844 15-15s-6.714844-15-15-15h-49.1875l-20-60h130.1875c24.8125 0 45-20.1875 45-45v-300c0-24.8125-20.1875-45-45-45zm-280.1875 450 20-60h98.375l20 60zm295.1875-105c0 8.269531-6.730469 15-15 15-22.660156 0-399.222656 0-422 0-8.269531 0-15-6.730469-15-15v-15h452zm0-45h-452v-255c0-8.269531 6.730469-15 15-15h422c8.269531 0 15 6.730469 15 15zm0 0"></path><path d="m301 75c0 8.285156-6.714844 15-15 15s-15-6.714844-15-15 6.714844-15 15-15 15 6.714844 15 15zm0 0"></path><path d="m241 75c0 8.285156-6.714844 15-15 15s-15-6.714844-15-15 6.714844-15 15-15 15 6.714844 15 15zm0 0"></path><path d="m361 75c0 8.285156-6.714844 15-15 15s-15-6.714844-15-15 6.714844-15 15-15 15 6.714844 15 15zm0 0"></path><path d="m166 60h-90c-8.285156 0-15 6.714844-15 15v180c0 8.285156 6.714844 15 15 15h90c8.285156 0 15-6.714844 15-15v-180c0-8.285156-6.714844-15-15-15zm-15 180h-60v-30h60zm0-60h-60v-30h60zm0-60h-60v-30h60zm0 0"></path><path d="m386.605469 214.394531c-5.855469-5.859375-15.355469-5.859375-21.210938 0l-17.0625 17.058594-49.851562-74.773437c-2.78125-4.171876-7.464844-6.679688-12.480469-6.679688s-9.699219 2.507812-12.480469 6.679688l-60 90c-4.59375 6.890624-2.734375 16.207031 4.160157 20.800781 6.863281 4.574219 16.183593 2.761719 20.800781-4.160157l47.519531-71.277343 47.519531 71.277343c2.492188 3.738282 6.53125 6.164063 11.003907 6.605469 4.476562.445313 8.90625-1.140625 12.082031-4.320312l19.394531-19.390625 19.394531 19.390625c5.855469 5.859375 15.355469 5.859375 21.210938 0 5.859375-5.855469 5.859375-15.351563 0-21.210938zm0 0"></path><path d="m406 90c-24.8125 0-45 20.1875-45 45s20.1875 45 45 45 45-20.1875 45-45-20.1875-45-45-45zm0 60c-8.269531 0-15-6.730469-15-15s6.730469-15 15-15 15 6.730469 15 15-6.730469 15-15 15zm0 0"></path></svg>

12. Napraviti da bude mobile (čak i odluka arhitekture da se prebacimo u MAUI pa imamo native mobile i nativ desktop - mozda poslje deploya ali onda svakako vidjeti kako ovo napraviti da bude dobro za mob u browseru jer je poanta da se koristi kamera i galeria mobitela za slanje slika/datoteka)
13. Chat mora imat preview graficke datoteke (slike itd)
14. Ako se dropa datoteka u chat neka bude kao lista datoteka koje su spremne za slanje i kada se pošalju neka poruka ima naziv datotkee i opciju za dowload - nije gotovo još
15. bug ako se radi s datotekama (download) onda na refreshu ticket page-a opet nudi dowload
16. Fix delete buttona na ticket dialogu
17. Gsap animacije? Cisto vidjeti pa nekako ugurati koju animaciju da se dobije osjećaj za framework, ne treba biti vatromet al cisto da vidimo kako se ponaša u real aplikacijama
18. Staviti aplikaciju na sustav metrike, dali postoje operacije koje se vrte u petlji i opterećuju bazu
19. Provjeriti migracijsku datoteku i dali svi pozivi u bazu podataka imaju smisla
20. Napraviti request i response objekte koje cemo slati i primati s APIja, ovako imamo prevelike argumente u funkcijama koji se odnose na isti objekt
21. Provjeriti šta znače oni SignalR eventi, koliko ja znam oni nebi trebali niti postojati (nisam siguran)
22. Priprema za deploy (micanje tesnih usera s logina?)
23. Maknuti layout inspo iz readme.md-a i mozda jos nabrijat tehničkim stvarima
24. Tu imamo ludorije s dockerima i treba napraviti jedan dobar za deploy
